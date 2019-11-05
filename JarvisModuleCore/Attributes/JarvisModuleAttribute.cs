using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisModuleCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JarvisModuleAttribute : Attribute
    {
        public string Name { get; }
        public JarvisModuleAttribute(string name)
        {
            Name = name;
        }
    }
}
