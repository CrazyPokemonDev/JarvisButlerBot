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
        Text = 1,
        Photo = 2,
        Audio = 4,
        Document = 8,
        Poll = 16,
        Video = 32,
        Voice = 64,
        All = 127
    }

    public delegate void ExecuteTask(Message message, Jarvis jarvis);
}
