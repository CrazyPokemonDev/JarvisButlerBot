using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
