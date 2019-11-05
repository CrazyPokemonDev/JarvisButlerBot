using JarvisModuleCore.Classes;
using System;

namespace JarvisModuleCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JarvisModuleAttribute : Attribute
    {
        /// <summary>
        /// The dependencies of this module, either as absolute paths or relative to the module managers lib folder.
        /// </summary>
        public string[] Dependencies { get; }

        /// <summary>
        /// Specifies that a class should be used as a module for the modular JARVIS bot. The class has to derive from <see cref="JarvisModule"/> and provide a parameterless constructor.
        /// </summary>
        /// <param name="dependencies"></param>
        public JarvisModuleAttribute(string[] dependencies = null)
        {
            Dependencies = dependencies ?? new string[0];
        }
    }
}
