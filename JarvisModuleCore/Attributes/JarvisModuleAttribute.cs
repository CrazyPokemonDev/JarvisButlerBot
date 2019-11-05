using JarvisModuleCore.Classes;
using System;

namespace JarvisModuleCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JarvisModuleAttribute : Attribute
    {
        /// <summary>
        /// The unique identifier of the module.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The dependencies of this module, either as absolute paths or relative to the module managers lib folder.
        /// </summary>
        public string[] Dependencies { get; }

        /// <summary>
        /// Specifies that a class should be used as a module for the modular JARVIS bot. The class has to derive from <see cref="JarvisModule"/>.
        /// </summary>
        /// <param name="id">A unique identifier for the module (used to recognize multiple different versions of the same module).</param>
        /// <param name="name">The name for the module that will be visible for the user.</param>
        /// <param name="dependencies">A list of filenames of the dependencies that should be copied for this module.
        /// Path can be either absolute or relative to the module managers lib folder.</param>
        public JarvisModuleAttribute(string id, string name, string[] dependencies = null)
        {
            Id = id;
            Name = name;
            Dependencies = dependencies ?? new string[0];
        }
    }
}
