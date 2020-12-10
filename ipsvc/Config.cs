using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ImproService
{
    class Config
    {
        public const string EventLogSource = "ipasvc";
        public const string EventLogName = "Improsec Password Auditor";

        public static string CurrentDirectory { get; set; } = null;
        public static bool Verbose { get; private set; } = false;
        public static bool ScanInactive { get; private set; } = true;

        public static void LoadSettings()
        {
            CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Verbose = bool.Parse(Read("verbose"));
            ScanInactive = bool.Parse(Read("scan_inactive"));

            Directory.CreateDirectory(CreatePath("data"));
            Directory.CreateDirectory(CreatePath("logs"));
            Directory.CreateDirectory(CreatePath("statistics"));
        }

        public static string Read(string name)
        {
            return ConfigurationManager.AppSettings.Get(name);
        }

        public static string CreatePath(string path)
        {
            return Path.Combine(CurrentDirectory, path);
        }

        public static string ReadFile(string directory, string name)
        {
            return Path.Combine(CreatePath(directory), Read(name));
        }

        public static string CreateLogFile(string directory, string prefix)
        {
            string folder = CreatePath(directory);
            string log = string.Format("{0}_{1}.txt", prefix, DateTime.Now.ToShortDateString().Replace('/', '_'));

            if (!Directory.Exists(folder))
                return null;
            else
                return Path.Combine(folder, log);
        }
    }
}
