//the CvL command line tool for installing, removing and updating programs
//(c) 2018 Christian E. "chrissx" Häußler
//licensed under GNU GPLv3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace cvl
{
    class Program
    {
        public static readonly string root_url = "https://chrissx.ga/cdn/cvl/";
        public static readonly string dict_url = root_url + "dic";
        public static readonly string cvl_path = Environment.GetEnvironmentVariable("CVLPATH", EnvironmentVariableTarget.User);
        public static readonly string bin_path = Path.Combine(cvl_path, "bin");
        public static readonly string ver_path = Path.Combine(cvl_path, "ver");
        public static readonly string dic_path = Path.Combine(cvl_path, "dic");
        public static readonly WebClient wc = new WebClient();
        public static readonly char lf = '\n';
        public static readonly byte lfbyte = Encoding.UTF8.GetBytes(new[] {lf})[0];

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

        static void fwrite(FileStream fs, object o)
        {
            fs.Write(Encoding.UTF8.GetBytes(o.ToString()), 0, o.ToString().Length);
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
                if (args.Length < 2)
                {
                    Console.WriteLine("Not enough arguments.");
                    Environment.Exit(1);
                }
                program[] programs = parse_dict(dic_path);
                List<program> prgs = new List<program>();
                for (int i = 1; i < args.Length; i++)
                {
                    program prg = programs.First((p) => p.name == args[i]);
                    if (prg == default(program))
                    {
                        Console.WriteLine($"Cannot find program {args[i]}.");
                        Environment.Exit(1);
                    }
                    prgs.Add(prg);
                }
                foreach (program prg in prgs)
                {
                    string path = Path.Combine(bin_path, prg.name + ".exe");
                    if(File.Exists(path))
                        File.Delete(path);
                    foreach (string m in prg.mirrors)
                    {
			            if(m == "")
				            continue;
                        Console.WriteLine($"Trying to download {prg.name} from {m}.");
                        try
                        {
                            wc.DownloadFile(m, path);
                            goto successfully_downloaded;
                        }
                        catch
                        {
                            Console.WriteLine("Unable to download from current mirror, moving on to next one...");
                        }
                    }
                    Console.WriteLine($"All the mirrors of {prg.name} failed.");
                    continue;
                successfully_downloaded:
                    File.WriteAllText(Path.Combine(ver_path, prg.name), prg.version.ToString());
                    Console.WriteLine($"Successfully downloaded {prg.name}.");
                }
            }
            else if (args[0] == "update")
            {
                List<program> programs = new List<program>();
                foreach (string url in download_utf8_lines(dict_url))
                {
                    if(url == "")
			    continue;
                    string[] p = download_utf8_lines(url);
                    string[] m = new string[p.Length - 2];
                    Array.Copy(p, 2, m, 0, m.Length);
                    programs.Add(new program(p[0], int.Parse(p[1]), m));
                }
                FileStream s = fopen(dic_path, FileAccess.Write);
                foreach (program p in programs)
                {
                    fwrite(s, p.name);
                    fwrite(s, " ");
                    fwrite(s, p.version);
                    foreach (string m in p.mirrors)
                    {
                        fwrite(s, " ");
                        fwrite(s, m);
                    }
                    fwrite(s, "\n");
                }
                s.Close();
            }
            else if(args[0] == "upgrade")
            {
                program[] programs = parse_dict(dic_path);
                foreach(string f in Directory.GetFiles(ver_path))
                {
                    string prg_name = Path.GetFileName(f);
                    program prg = programs.First((p) => p.name == prg_name);
                    if (prg == default(program))
                    {
                        Console.Write($"Package {prg_name} does no longer exist in the database, ");
                        Console.Write("it will be left on your machine, but you won't get upgrades, ");
                        Console.WriteLine("from now on it won't be checked by CvL anymore.");
                        File.Delete(f);
                    }
                    else
                    {
                        int cv = int.Parse(File.ReadAllText(f));
                        int nv = prg.version;
                        if (nv > cv)
                        {
                            Console.WriteLine($"Upgrading {prg_name}...");
                            string tmp = Path.GetTempFileName();
                            foreach (string m in prg.mirrors)
                            {
				    if(m == "")
					    continue;
                                try
                                {
                                    wc.DownloadFile(m, tmp);
                                    goto success;
                                }
                                catch{}
                            }
                            Console.WriteLine($"All mirrors for {prg_name} failed, leaving old version there.");
                            continue;
                        success:
                            string prg_bin = Path.Combine(bin_path, prg_name + ".exe");
                            File.Delete(prg_bin);
                            File.Copy(tmp, prg_bin);
                            File.WriteAllText(f, nv.ToString());
                            Console.WriteLine($"Successfully upgraded {prg_name}.");
                        }
                    }
                }
            }
            else if(args[0] == "remove")
            {

            }
            else
            {

            }
        }

        static program[] parse_dict(string file)
        {
            List<program> programs = new List<program>();
            int i;
            FileStream s = File.Open(file, FileMode.Open, FileAccess.Read);
            while ((i = s.ReadByte()) != -1)
            {
                string name = null;
                int version = -1;
                List<string> mirrors = new List<string>();
                StringBuilder str = new StringBuilder(Encoding.UTF8.GetString(new[] {(byte)i}));
                while ((i = s.ReadByte()) != lfbyte)
                {
                    char c = Encoding.UTF8.GetChars(new[] {(byte)i})[0];
                    if(c == ' ')
                    {
                        if(name == null)
                            name = str.ToString();
                        else if(version == -1)
                            version = int.Parse(str.ToString());
                        else
                            mirrors.Add(str.ToString());
                        str.Clear();
                    }
                    else
                        str.Append(c);
                }
                mirrors.Add(str.ToString());
                programs.Add(new program(name, version, mirrors.ToArray()));
            }
            GC.Collect();
            return programs.ToArray();
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
        static bool arrequ(string[] s1, string[] s2)
        {
            if (s1.LongLength != s2.LongLength)
                return false;
            for (long i = 0; i < s1.LongLength; i++)
                if (s1[i] != s2[i])
                    return false;
            return true;
        }
        public static bool operator==(program p1, program p2)
        {
            return p1.name == p2.name && p1.version == p2.version && arrequ(p1.mirrors, p2.mirrors);
        }
        public static bool operator!=(program p1, program p2)
        {
            return !(p1 == p2);
        }
    }
}
