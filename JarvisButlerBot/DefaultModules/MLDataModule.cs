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
using JarvisButlerBot.Helpers;
using System.IO;
using File = System.IO.File;
using Newtonsoft.Json;
using Telegram.Bot.Types.InputFiles;

namespace JarvisButlerBot.DefaultModules
{
    [JarvisModule]
    public class MLDataModule : JarvisModule
    {
        public override string Id => "jarvis.default.mldata";
        public override string Name => "ML Data Generation";
        public override Version Version => Version.Parse("1.0.0");
        public override TaskPredictionInput[] MLTrainingData => TrainingData.MLData;
        private readonly Dictionary<int, List<TaskPredictionInput>> learning = new Dictionary<int, List<TaskPredictionInput>>();
        private readonly List<Message> initialMessages = new List<Message>();
        private Jarvis jarvis;

        public override void Start(Jarvis jarvis)
        {
            base.Start(jarvis);
            this.jarvis = jarvis;
            jarvis.OnMessage += Jarvis_OnMessage;
        }

        private async void Jarvis_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (!learning.ContainsKey(e.Message.From.Id)) return;
                if (initialMessages.Contains(e.Message))
                {
                    initialMessages.Remove(e.Message);
                    return;
                }
                learning[e.Message.From.Id].Add(new TaskPredictionInput
                {
                    MessageText = e.Message.GetText().PrepareForPrediction(e.Message.GetEntities(), jarvis.Username),
                    MessageType = e.Message.GetMessageType().ToString(),
                    ChatType = e.Message.Chat.GetChatType().ToString(),
                    HasReplyToMessage = (e.Message.ReplyToMessage != null).ToString(),
                    TaskId = "%taskid%"
                });
                if (learning[e.Message.From.Id].Count >= 50 && !jarvis.IsGlobalAdmin(e.Message.From.Id))
                {
                    StopLearning(e.Message, jarvis);
                    return;
                }
            }
            catch (Exception ex)
            {
                await jarvis.ReplyAsync(e.Message, "An error occurred saving this message:\n" + ex);
            }
        }

        [JarvisTask("jarvis.default.mldata.startlearning", Command = "/startlearning", PossibleMessageTypes = PossibleMessageTypes.AllExceptPoll)]
        public async void StartLearning(Message message, Jarvis jarvis)
        {
            if (learning.ContainsKey(message.From.Id)) return;
            initialMessages.Add(message);
            learning.Add(message.From.Id, new List<TaskPredictionInput>());
            await jarvis.ReplyAsync(message, "Alright, I will start saving all the messages I receive from you until you tell me to stop.");
        }

        [JarvisTask("jarvis.default.mldata.stoplearning", Command = "/stoplearning", PossibleMessageTypes = PossibleMessageTypes.AllExceptPoll)]
        public async void StopLearning(Message message, Jarvis jarvis)
        {
            if (!learning.ContainsKey(message.From.Id))
            {
                await jarvis.ReplyAsync(message, "I didn't even start saving your messages yet...");
                return;
            }
            var learnedData = learning[message.From.Id];
            learning.Remove(message.From.Id);
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, MakeCsharpCode(learnedData.ToArray()));
            using (var stream = File.OpenRead(tempFilePath))
            {
                await jarvis.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, "learned.txt"), caption: "Okay, here is all the data I collected.");
            }
        }

        public string MakeCsharpCode(TaskPredictionInput[] data)
        {
            return $"new TaskPredictionInput[] {{\n{string.Join(",\n", data.Select(x => "\t" + MakeCsharpCode(x)))}\n}}";
        }

        public string MakeCsharpCode(TaskPredictionInput obj)
        {
            return $"new TaskPredicitionInput{{ MessageText = \"{obj.MessageText}\", MessageType = \"{obj.MessageType}\", " +
                $"ChatType = \"{obj.ChatType}\", HasReplyToMessage = \"{obj.HasReplyToMessage}\", TaskId = \"{obj.TaskId}\" }}";
        }
    }
}
