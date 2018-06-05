using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace CvL
{
    static class Program
    {
        static void Main()
        {
            while (true)
            {
                string[] args = ReadLine().parse_args();
                switch (args[0])
                {
                    case "shutdown"
                    default: WriteLine($"Unknown command {args[0]}."); break;
                }
            }
        }

        static string[] parse_args(this string s)
        {
            string[] args_raw = s.Split(' ');
            List<string> args = new List<string>();
            bool add = false;
            foreach (string a in args_raw)
                if (add)
                {
                    args[args.Count - 1] += $" {a.Replace("\"", "")}";
                    add = !a.Contains("\"");
                }
                else
                {
                    args.Add(a.Replace("\"", ""));
                    add = a.Contains("\"");
                }
            return args.ToArray();
        }
    }
}
