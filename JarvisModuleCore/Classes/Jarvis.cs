using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JarvisModuleCore.Classes
{
    /// <summary>
    /// Telegram bot client with a little bit of extra functionality
    /// </summary>
    public class Jarvis : Telegram.Bot.TelegramBotClient
    {
        public List<int> GlobalAdmins { get; } = new List<int>();
        public string Username { get; set; } = null;
        public new event EventHandler<MessageEventArgs> OnMessage;
        private readonly List<(Message Msg, int[] UserWhitelist)> dontRedirectAnswers = new List<(Message Msg, int[] UserWhitelist)>();

        /// <summary>
        /// Creates a new instance of the control part for JARVIS.
        /// </summary>
        /// <param name="token">The telegram bot token of the bot to use</param>
        /// <param name="globalAdmins">A list of telegram IDs to be added as initial global admins.</param>
        public Jarvis(string token, int[] globalAdmins = null) : base(token)
        {
            base.OnMessage += Jarvis_OnMessageRedirect;
            if (globalAdmins != null) foreach (int id in globalAdmins) GlobalAdmins.Add(id);
            GetMeAsync().ContinueWith(x => Username = x.Result.Username);
        }

        private void Jarvis_OnMessageRedirect(object sender, MessageEventArgs e)
        {
            if (e.Message.ReplyToMessage == null)
            {
                OnMessage.Invoke(sender, e);
                return;
            }
            if (dontRedirectAnswers.Any(x => e.Message.ReplyToMessage.MessageId == x.Msg.MessageId && e.Message.Chat.Id == x.Msg.Chat.Id
                && (x.UserWhitelist == null || x.UserWhitelist.Contains(e.Message.From.Id)))) return;
            OnMessage.Invoke(sender, e);
        }

        /// <summary>
        /// Whether the user with the given ID is a global admin for this instance of JARVIS.
        /// </summary>
        /// <param name="id">The telegram ID of the user in question.</param>
        /// <returns><see langword="true"/> if the user is a global admin, else <see langword="false"/>.</returns>
        public bool IsGlobalAdmin(int id)
        {
            return GlobalAdmins.Contains(id);
        }

        /// <summary>
        /// Sends a text message to the specified chat and then waits for the next user to reply to it, then returns the reply message.
        /// </summary>
        /// <param name="chatId">The id of the chat to send the message to</param>
        /// <param name="text">The text of the message to send</param>
        /// <param name="parseMode">The parse mode of the message</param>
        /// <param name="disableWebPagePreview">Whether to disable the preview of webpages</param>
        /// <param name="disableNotification">Whether to send the message with a silent notification</param>
        /// <param name="replyToMessageId">The message id to reply to, if any</param>
        /// <param name="replyMarkup">The message reply markup</param>
        /// <param name="forceReply">Whether to use a <see cref="ForceReplyMarkup"/>{Selective = true} as the reply markup. Will be ignored if <paramref name="replyMarkup"/> isn't null.</param>
        /// <param name="userWhitelist">If this is present, only returns on messages from one of these users. They are identified by their telegram ID.</param>
        /// <param name="cancellationToken">A cancellation token for the http client and the waiting process</param>
        /// <returns>The reply message.</returns>
        public async Task<Message> SendTextMessageAndWaitForReplyAsync(ChatId chatId, string text, ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false,
            bool disableNotification = false, int replyToMessageId = 0, IReplyMarkup replyMarkup = null, bool forceReply = true,
            int[] userWhitelist = null, CancellationToken cancellationToken = default)
        {
            if (replyMarkup == null && forceReply) replyMarkup = new ForceReplyMarkup() { Selective = true };
            Message sentMessage = null;
            Message replyMessage = null;
            using (ManualResetEventSlim mre = new ManualResetEventSlim(false))
            {
                void messageHandler(object sender, MessageEventArgs e)
                {
                    Task.Run(() =>
                    {
                        while (sentMessage == null) { };
                        if (e.Message.ReplyToMessage?.MessageId == sentMessage.MessageId && e.Message.Chat.Id == sentMessage.Chat.Id
                            && (userWhitelist == null || userWhitelist.Contains(e.Message.From.Id)))
                        {
                            replyMessage = e.Message;
                            mre.Set();
                        }
                    });
                }
                OnMessage += messageHandler;
                sentMessage = await SendTextMessageAsync(chatId, text, parseMode, disableWebPagePreview, disableNotification, replyToMessageId, replyMarkup, cancellationToken);
                (Message sentMessage, int[] userWhitelist) tuple = (sentMessage, userWhitelist);
                dontRedirectAnswers.Add(tuple);
                WaitHandle.WaitAny(new WaitHandle[] { cancellationToken.WaitHandle, mre.WaitHandle });
                dontRedirectAnswers.Remove(tuple);
                OnMessage -= messageHandler;
                return replyMessage;
            }
        }

        /// <summary>
        /// Sends a text message as a reply to the given message.
        /// </summary>
        /// <param name="message">The message to reply to.</param>
        /// <param name="text">The text of the message to send</param>
        /// <param name="parseMode">The parse mode of the message</param>
        /// <param name="disableWebPagePreview">Whether to disable the preview of webpages</param>
        /// <param name="disableNotification">Whether to send the message with a silent notification</param>
        /// <param name="replyMarkup">The message reply markup</param>
        /// <param name="cancellationToken">A cancellation token for the http client.</param>
        /// <returns>The reply message.</returns>
        public async Task<Message> ReplyAsync(Message message, string text, ParseMode parseMode = ParseMode.Default, bool disableWebPagePreview = false,
            bool disableNotification = false, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await SendTextMessageAsync(message.Chat.Id, text, parseMode, disableWebPagePreview, disableNotification, message.MessageId, replyMarkup, cancellationToken);
        }
    }
}
