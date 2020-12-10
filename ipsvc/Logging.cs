using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImproService
{
    class Logging
    {
        private static readonly object Lock = new object();

        public static void WriteEventLog(string message, EventLogEntryType type = EventLogEntryType.Information, int id = 0)
        {
            if (type != EventLogEntryType.Information || Config.Verbose)
            {
                if (EventLog.SourceExists(Config.EventLogSource))
                    EventLog.WriteEntry(Config.EventLogSource, message, type, id);
            }
        }

        public static void WriteLog(string source, string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            string severity = string.Empty;

            switch (type)
            {
                case EventLogEntryType.Information:
                    severity = (Config.Verbose ? "information" : string.Empty);
                    break;
                case EventLogEntryType.Warning:
                    severity = "warning";
                    break;
                case EventLogEntryType.Error:
                    severity = "error";
                    break;
            }

            if (!string.IsNullOrEmpty(severity))
            {
                string timestamp = DateTime.Now.ToString();
                string content = string.Format("[{0}][{1}] {2}: {3}\n", timestamp, severity, source, message);

                lock (Lock)
                {
                    File.AppendAllText(Config.CreateLogFile("logs", "log"), content);
                }
            }
        }
    }
}
