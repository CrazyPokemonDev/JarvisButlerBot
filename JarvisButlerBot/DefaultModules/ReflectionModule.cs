using JarvisButlerBot.Helpers;
using JarvisButlerBot.Training;
using JarvisModuleCore.Attributes;
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JarvisButlerBot.DefaultModules
{
    [JarvisModule]
    public class ReflectionModule : JarvisModule
    {
        public override string Id => "jarvis.default.reflection";
        public override string Name => "Reflection module";
        public override Version Version => Version.Parse("1.0.0");
        public override TaskPredictionInput[] MLTrainingData => TrainingData.Reflection;
        private Jarvis jarvis;
        private const int modulesPerPage = 3;

        public override void Start(Jarvis jarvis)
        {
            base.Start(jarvis);
            jarvis.OnCallbackQuery += Jarvis_OnCallbackQuery;
            this.jarvis = jarvis;
        }

        #region Module list
        [JarvisTask("jarvis.default.reflection.modulelist", Command = "/modules", PossibleMessageTypes = PossibleMessageTypes.AllExceptPoll)]
        public async void ModuleList(Message message, Jarvis jarvis)
        {
            string modulePage1 = "";
            for (int i = 0; i < Math.Min(Program.Modules.Count, modulesPerPage); i++)
            {
                modulePage1 += GetModuleInfo(Program.Modules[i]) + "\n\n";
            }
            InlineKeyboardMarkup replyMarkup = GetReplyMarkup(0);
            await jarvis.ReplyAsync(message, modulePage1, replyMarkup: replyMarkup, parseMode: ParseMode.Html);
        }

        private string GetModuleInfo(JarvisModule module)
        {
            return $"<b>{module.Name.EscapeHtml()}</b> (<code>{module.Id.EscapeHtml()}</code>):\nVersion {module.Version}";
        }

        private InlineKeyboardMarkup GetReplyMarkup(int currentPage)
        {
            var pageCount = (Program.Modules.Count / modulesPerPage) + 1;
            if (pageCount < 2) return null;
            var buttonBack = new InlineKeyboardButton { CallbackData = $"modulePage:{currentPage - 1}", Text = "⬅️" };
            var buttonNext = new InlineKeyboardButton { CallbackData = $"modulePage:{currentPage + 1}", Text = "➡️" };
            if (currentPage == pageCount - 1) return new InlineKeyboardMarkup(buttonBack);
            if (currentPage == 0) return new InlineKeyboardMarkup(buttonNext);
            return new InlineKeyboardMarkup(new InlineKeyboardButton[] { buttonBack, buttonNext });
        }

        private async void Jarvis_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Message == null || !e.CallbackQuery.Data.StartsWith("modulePage:") || !int.TryParse(e.CallbackQuery.Data.Split(':')[1], out int page)) return;
            var modulePage = "";
            for (int i = page * modulesPerPage; i < Math.Min(Program.Modules.Count, (page + 1) * modulesPerPage); i++)
            {
                modulePage += GetModuleInfo(Program.Modules[i]) + "\n\n";
            }
            await jarvis.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
            await jarvis.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId, modulePage, replyMarkup: GetReplyMarkup(page), parseMode: ParseMode.Html);
        }
        #endregion
    }
}
