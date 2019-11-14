using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisCoreTests
{
    public static class Extensions
    {
        public static T GetRandomElement<T>(this T[] array, Random rnd = null)
        {
            if (rnd == null) rnd = new Random();
            if (array.Length < 1) throw new IndexOutOfRangeException("Array didn't contain an element!");
            return array[rnd.Next(array.Length)];
        }
    }
}
