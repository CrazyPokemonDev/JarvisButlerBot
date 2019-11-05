using JarvisModuleCore.ML;
using System;

namespace JarvisModuleCore.Classes
{
    /// <summary>
    /// The base class for all JARVIS modules.
    /// </summary>
    public abstract class JarvisModule
    {
        public abstract string Id { get; }
        /// <summary>
        /// The name of the module.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// The version of this module.
        /// </summary>
        public abstract Version Version { get; }
        /// <summary>
        /// A set of training data that will be used to teach JARVIS which task to execute on what input.
        /// Recommended row count is 50 per task.
        /// </summary>
        public abstract TaskPredictionInput[] MLTrainingData { get; }
        /// <summary>
        /// This method will be called once the module is loaded and JARVIS starts up.
        /// </summary>
        /// <param name="jarvis"></param>
        public virtual void Start(Jarvis jarvis)
        {

        }
    }
}
