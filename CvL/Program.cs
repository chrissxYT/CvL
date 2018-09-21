//the CvL command line tool for installing, removing and updating programs
//(c) 2018 Christian E. "chrissx" Häußler
//licensed under GNU GPLv3
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace cvl
{
    class Program
    {
        public static readonly string root_url = "https://chrissx.domain_will_be_here/cvlbin/";
        public static readonly string dict_url = root_url + "dic";
        public static readonly string cvl_path = Environment.GetEnvironmentVariable("CVLPATH", EnvironmentVariableTarget.User);
        public static readonly string bin_path = Path.Combine(cvl_path, "bin");
        public static readonly string dic_path = Path.Combine(cvl_path, "dic");
        public static readonly WebClient wc = new WebClient();

        static string[] download_utf8_lines(string url)
        {
            return Encoding.UTF8.GetString(wc.DownloadData(url)).Replace("\r", "").Split('\n');
        }

        static FileStream fopen(string file, FileAccess access)
        {
            return File.Open(file, access == FileAccess.Read ? FileMode.Open : access == FileAccess.Write ? FileMode.Create : FileMode.Append, access);
        }

        static void fwrite(FileStream fs, string s)
        {
            fs.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Not enough arguments.");
                Environment.Exit(1);
            }
            if (args[0] == "install")
            {

            }
            else if (args[0] == "update")
            {
                List<program> programs = new List<program>();
                foreach (string url in download_utf8_lines(dict_url))
                {
                    string[] p = download_utf8_lines(url);
                    string[] m = new string[p.Length - 2];
                    Array.Copy(p, 2, m, 0, m.Length);
                    programs.Add(new program(p[0], int.Parse(p[1]), m));
                }
                FileStream s = fopen(dic_path, FileAccess.Write);
                StringBuilder b = new StringBuilder();
                foreach (program p in programs)
                {
                    b.Append(p.name + " " + p.version);
                    foreach (string m in p.mirrors)
                    {
                        b.Append(" ");
                        b.Append(m);
                    }
                    b.Append("\n");
                }
                fwrite(s, b.ToString());
                s.Close();
            }
            else if(args[0] == "upgrade")
            {

            }
            else if(args[0] == "remove")
            {

            }
            else
            {

            }
        }
    }

    struct program
    {
        public string[] mirrors;
        public string name;
        public int version;
        public program(string name, int version, string[] mirrors)
        {
            this.name = name;
            this.version = version;
            this.mirrors = mirrors;
        }
    }
}
