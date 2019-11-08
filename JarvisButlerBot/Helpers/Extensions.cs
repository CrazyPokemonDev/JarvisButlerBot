using JarvisModuleCore.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JarvisButlerBot.Helpers
{
    public static class Extensions
    {
        public static void Shuffle<T>(this List<T> list, int? seed = null)
        {
            Random random;
            if (seed.HasValue) random = new Random(seed.Value);
            else random = new Random();
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string EscapeHtml(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public static string GetText(this Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                case MessageType.Voice:
                case MessageType.Photo:
                case MessageType.Video:
                case MessageType.Document:
                    return message.Caption;
                case MessageType.Poll:
                    return message.Poll.Question;
                case MessageType.Text:
                    return message.Text;
                default:
                    return null;
            }
        }

        public static MessageEntity[] GetEntities(this Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                case MessageType.Voice:
                case MessageType.Photo:
                case MessageType.Video:
                case MessageType.Document:
                    return message.CaptionEntities ?? new MessageEntity[0];
                case MessageType.Text:
                    return message.Entities ?? new MessageEntity[0];
                default:
                    return new MessageEntity[0];
            }
        }

        public static PossibleMessageTypes GetMessageType(this Message message)
        {
            switch (message.Type)
            {
                case MessageType.Audio:
                    return PossibleMessageTypes.Audio;
                case MessageType.Voice:
                    return PossibleMessageTypes.Voice;
                case MessageType.Photo:
                    return PossibleMessageTypes.Photo;
                case MessageType.Video:
                    return PossibleMessageTypes.Video;
                case MessageType.Document:
                    if (message.Animation != null) return PossibleMessageTypes.Animation;
                    else return PossibleMessageTypes.Document;
                case MessageType.Poll:
                    return PossibleMessageTypes.Poll;
                case MessageType.Text:
                    return PossibleMessageTypes.Text;
                default:
                    return PossibleMessageTypes.All;
            }
        }

        public static PossibleChatTypes GetChatType(this Chat chat)
        {
            switch (chat.Type)
            {
                case ChatType.Private:
                    return PossibleChatTypes.Private;
                case ChatType.Group:
                    return PossibleChatTypes.Group;
                case ChatType.Channel:
                    return PossibleChatTypes.Channel;
                case ChatType.Supergroup:
                    return PossibleChatTypes.Supergroup;
                default:
                    return PossibleChatTypes.All;
            }
        }

        public static string PrepareForPrediction(this string text, MessageEntity[] entities, string username)
        {
            foreach (var entity in entities)
            {
                switch (entity.Type)
                {
                    case MessageEntityType.Mention:
                        if (text.Substring(entity.Offset, entity.Length).ToLower() == "@" + username.ToLower()) continue;
                        text = text.Substring(0, entity.Offset) + "@User" + text.Substring(entity.Offset + entity.Length);
                        foreach (var e in entities.Except(new MessageEntity[] { entity }))
                        {
                            if (e.Offset > entity.Offset) e.Offset -= entity.Length - 5;
                        }
                        break;
                    case MessageEntityType.TextMention:
                        text = text.Substring(0, entity.Offset) + "@Mention" + text.Substring(entity.Offset + entity.Length);
                        foreach (var e in entities.Except(new MessageEntity[] { entity }))
                        {
                            if (e.Offset > entity.Offset) e.Offset -= entity.Length - 8;
                        }
                        break;
                }
            }
            text = Regex.Replace(text, Regex.Escape($"@{username}".ToLower()), "@Username", RegexOptions.IgnoreCase);
            return text;
        }
    }
}
