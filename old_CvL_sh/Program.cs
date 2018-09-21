//original CvL shell
//(c) 2018 Christian E. "chrissx" Häußler
//licensed under GNU GPLv3
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static System.Console;

namespace CvL
{
    delegate void cmd_func(string[] args);
    
    struct Command
    {
        public Command(string name, string syntax, string info, cmd_func func)
        {
            this.name = name;
            this.syntax = syntax;
            this.info = info;
            this.func = func;
        }

        public string name;
        public string syntax;
        public string info;
        public cmd_func func;
    }

    static class Program
    {
        static Command[] cmds = new Command[]
        {
            new Command("info", "\ninfo\ninfo [command]\n", "Shows the details of the given command. (this screen)", info),
            new Command("kill", "kill [name / id] {-f} {-t} {-pid}", "Kills the specified process.\n-f forces quit\n-t also quits all the child processes\n-pid sets the [name / id] to use PID", kill),
            new Command("zip", "zip [output file] [list of entries] {-64}", "Compresses all the entries into the output file.\n-64 enables use of Zip64\nAn entry is encoded like this: [input file] [entry name] [compression method] {-u}\n-u specifies the use of unicode for the entry name\nUsable compression methods:\ndeflate\ndeflate64\nstore\nbzip2", zip),
            new Command("gzip", "gzip [input file] [output file]", "Compresses the input file into the output file in the gzip-format.", gzip)
        };

        static readonly string username = Registry.GetValue("HKEY_CURRENT_USER\\Volatile Environment", "USERNAME", "_NAMENOTAVAILABLE_").ToString();
        static readonly string pc_name = Registry.GetValue("HKEY_CURRENT_USER\\Volatile Environment", "USERDOMAIN", "_NAMENOTAVAILABLE_").ToString();
        static readonly string software_version = "beta 1";
        
        static void Main()
        {
            SetWindowSize(150, 40);
            WriteLine($"<<CvL Shell>> [{software_version}]");
            WriteLine("(c) 2018 chrissx Media Inc.");
            WriteLine();
            while (true)
            {
                Write($"{username}@{pc_name} $");
                string[] args = ReadLine().parse_args();
                string cmd = args[0].ToLower();
                bool u = true;
                foreach (Command c in cmds)
                    if (c.name == cmd)
                        try
                        {
                            u = false;
                            c.func(args);
                        }
                        catch (Exception e)
                        {
                            WriteLine(e);
                        }
                if (cmd == "stop" || cmd == "shutdown")
                    return;
                else if (u)
                    WriteLine($"Unknown command {args[0]}.");
            }
        }

        static void gzip(string[] args)
        {
            Stream i = File.Open(args[1].Replace("\"", ""), FileMode.Open, FileAccess.Read);
            Stream o = new GZipOutputStream(File.Open(args[2].Replace("\"", ""), FileMode.Create, FileAccess.Write));
            StreamUtils.Copy(i, o, new byte[1024*1024*32]);
            i.Close();
            i.Dispose();
            o.Close();
            o.Dispose();
            GC.Collect();
        }

        static void info(string[] args)
        {
            if (args.Length == 1)
            {
                foreach (Command cmd in cmds)
                    WriteLine(cmd.name);
            }
            else
            {
                Command? temp_c = cmds.get(args[1]);
                Command c = temp_c ?? throw new Exception($"Unknown command {args[1]}.");
                Clear();
                WriteLine(c.name + " info:");
                WriteLine();
                WriteLine("Syntax: " + c.syntax);
                WriteLine();
                WriteLine("Description:");
                WriteLine(c.info);
                ReadLine();
                Clear();
            }
        }

        static void kill(string[] args)
        {
            bool f = false;
            bool t = false;
            bool p = false;
            foreach (string a in args)
                if (a == "-f")
                    f = true;
                else if (a == "-t")
                    t = true;
                else if (a == "-pid")
                    p = true;
            run_win32_cmd("taskkill", $"{(p ? "/pid" : "/im")} {args[1]}{(f ? " /f" : "")}{(t ? " /t" : "")}");
        }

        static void zip(string[] args)
        {
            bool _64 = false;
            foreach (string a in args)
                if (a == "-64")
                    _64 = true;
            ZipFile zip = new ZipFile(args[1].Replace("\"", ""));
            zip.UseZip64 = _64 ? UseZip64.On : UseZip64.Off;
            byte b = 3;
            for (int i = 2; i < args.Length + b; i += b)
            {
                bool u = args.Length > i + 3 && args[i + 3] == "-u";
                b = u ? (byte)4 : (byte)3;
                string cm_str = args[i + 2].ToLower();
                CompressionMethod cm = cm_str == "deflate" ? CompressionMethod.Deflated : cm_str == "deflate64" ? CompressionMethod.Deflate64 : cm_str == "store" ? CompressionMethod.Stored : cm_str.StartsWith("bz") ? CompressionMethod.BZip2 : throw new Exception($"Unknown compression method {cm_str}.");
                zip.Add(new StaticDiskDataSource(args[i].Replace("\"", "")), args[i + 1], cm, u);
            }
            zip.Close();
            zip = null;
            GC.Collect();
        }

        static void run_win32_cmd(string cmd, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(cmd, args);
            psi.CreateNoWindow = true;
            psi.ErrorDialog = true;
            psi.RedirectStandardError = false;
            psi.RedirectStandardInput = false;
            psi.RedirectStandardOutput = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
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

        static Command? get(this Command[] cmds, string name)
        {
            foreach (Command c in cmds)
                if (c.name == name)
                    return c;
            return null;
        }

        static bool has(this Command[] cmds, string name)
        {
            foreach (Command c in cmds)
                if (c.name == name)
                    return true;
            return false;
        }
    }
}

//LEGACY CODE (the "by chrissx/chrissx Media Inc" is just to beautiful):
//WriteLine("Booting <<CvL Shell>>...");
//sleep(2);
//WriteLine("■                   ■         ■                        ■       ■         ■                                     ■ ■");
//sleep((decimal) 0.5);
//WriteLine("■                   ■           ■■■  ■■■ ■   ■        ■        ■           ■■■ ■■■  ■   ■       ■■ ■■          ■             ■■■");
//sleep((decimal)0.5);
//WriteLine("■■■■■ ■  ■      ■■■ ■■■■ ■■■■ ■ ■    ■    ■ ■        ■     ■■■ ■■■■ ■■■■ ■ ■   ■     ■ ■       ■  ■  ■ ■■■■ ■■■■ ■    ■       ■  ■■■■  ■■■");
//sleep((decimal)0.5);
//WriteLine("■   ■ ■ ■      ■    ■  ■ ■    ■ ■■■  ■■■   ■        ■     ■    ■  ■ ■    ■ ■■■ ■■■    ■        ■ ■■  ■ ■  ■ ■  ■ ■ ■■■■       ■  ■  ■ ■");
//sleep((decimal)0.5);
//WriteLine("■   ■  ■■      ■    ■  ■ ■    ■   ■    ■  ■ ■      ■      ■    ■  ■ ■    ■   ■   ■   ■ ■       ■ ■   ■ ■■■■ ■  ■ ■ ■  ■       ■  ■  ■ ■");
//sleep((decimal)0.5);
//WriteLine("■■■■■  ■        ■■■ ■  ■ ■    ■ ■■■  ■■■ ■   ■    ■        ■■■ ■  ■ ■    ■ ■■■ ■■■  ■   ■      ■     ■  ■■■ ■■■■ ■ ■■■■      ■■■ ■  ■  ■■■  ■");
//sleep((decimal)0.5);
//WriteLine("      ■");
//sleep(3);
//Clear();
//static void sleep(decimal time)
//{
//    time *= 10000000;
//    for (decimal i = 0; i < time; i++)
//        ;
//}