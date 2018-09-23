//CvL setup tool
//(just a linear installer that installs CvL for the current user)
//(c) 2018 Christian E. "chrissx" Häußler
//licensed under GNU GPLv3
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace CvLSetup
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Installing CvL...");
            Console.WriteLine("Seetting up internal variables...");
            string tool_url = "https://chrissx.ga/cdn/cvl/cvl.exe";
            string appdata = Registry.CurrentUser.OpenSubKey("Volatile Environment", false).GetValue("APPDATA").ToString();
            string cvl = Path.Combine(appdata, "CvL");
            string bin = Path.Combine(cvl, "bin");
            string tool_path = Path.Combine(bin, "cvl.exe");
            string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            path += bin + ";";
            Console.WriteLine("Creating directories...");
            Directory.CreateDirectory(cvl);
            Directory.CreateDirectory(bin);
            Console.WriteLine("Adding to Path...");
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.User);
            Console.WriteLine("Setting CVLPATH...");
            Environment.SetEnvironmentVariable("CVLPATH", cvl, EnvironmentVariableTarget.User);
            Console.WriteLine("Installing the CvL tool...");
            new WebClient().DownloadFile(tool_url, tool_path);
            Console.WriteLine("Downloading program information...");
            Process.Start(tool_path, "update");
            Console.WriteLine("Done setting up CvL.");
            Console.WriteLine();
            Console.WriteLine("Here's a quick tutorial for managing programs:");
            Console.WriteLine(" cvl update to update program information.");
            Console.WriteLine(" cvl upgrade to upgrade outdated programs.");
            Console.WriteLine(" cvl install <program> to install <program>.");
            Console.WriteLine(" cvl remove <program to remove <program>.");
            Console.WriteLine();
            Console.WriteLine("Example - Installing and removing tar:");
            Console.WriteLine(" cvl update");
            Console.WriteLine(" cvl install tar");
            Console.WriteLine(" cvl remove tar");
            Console.Write("Press any key to close...");
            Console.ReadKey();
        }
    }
}
