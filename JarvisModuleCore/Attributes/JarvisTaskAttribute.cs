using JarvisModuleCore.Classes;
using System;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JarvisModuleCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class JarvisTaskAttribute : Attribute
    {
        /// <summary>
        /// The unique identifier for this task.
        /// </summary>
        public string TaskId { get; }
        /// <summary>
        /// The possible message types for this task.
        /// </summary>
        public PossibleMessageTypes PossibleMessageTypes { get; set; } = PossibleMessageTypes.All;
        /// <summary>
        /// The possible chat types in which the message can be sent
        /// </summary>
        public PossibleChatTypes PossibleChatTypes { get; set; } = PossibleChatTypes.All;
        public string[] Commands { get; set; } = new string[0];
        /// <summary>
        /// Alternative setter for only one command string. Will return the first command of the list or null if the list is empty.
        /// </summary>
        public string Command { set { Commands = new string[] { value }; } get { return Commands.Length > 0 ? Commands[0] : null; } }
        /// <summary>
        /// Specifies that a method should be treated as an executable task for the modular JARVIS butler bot. The method must be of the <see cref="ExecuteTask"/> delegate type.
        /// </summary>
        /// <param name="taskId">A unique identifier for this task.</param>
        public JarvisTaskAttribute(string taskId)
        {
            TaskId = taskId;
        }
    }

    [Flags]
    public enum PossibleMessageTypes
    {
        None = 0,
        Text = 1,
        Photo = 2,
        Audio = 4,
        Document = 8,
        Poll = 16,
        Video = 32,
        Voice = 64,
        Animation = 128,
        Media = Photo | Audio | Document | Video | Voice | Animation,
        AllExceptPoll = All ^ Poll,
        All = 255
    }

    public enum PossibleChatTypes
    {
        None = 0,
        Group = 1,
        Supergroup = 2,
        AnyGroup = 3,
        Private = 4,
        Channel = 8,
        All = 15
    }

    public delegate void ExecuteTask(Message message, Jarvis jarvis);
}
