using DSInternals.Common.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImproService
{
    class Statistics
    {
        public static void CreateStatistics(List<DSAccount> users, List<DSAccount> leaks)
        {
            var logfile = Config.CreateLogFile("statistics", "stat");

            var valids = users
                .Where(u => u.NTHash != null)
                .ToList();

            var invalids = users
                .Where(u => u.NTHash == null)
                .ToList();

            /* Calculate statistics for empty passwords */
            var empty = valids
                .Where(u => Utility.CompareEmpty(u.NTHash))
                .ToList();

            /* Calculate statistics for shared passwords */
            var shared = valids
                .GroupBy(u => u.NTHash, new Utility.ByteArrayComparer())
                .Where(u => u.Count() > 1)
                .OrderByDescending(u => u.Count());

            /* Compute statistics table */
            bool pred_enabled(DSAccount u) => u.Enabled == true;
            bool pred_disabled(DSAccount u) => u.Enabled == false;

            var stats = new List<List<Tuple<string, int>>>
            {
                new List<Tuple<string, int>>()
                {
                    new Tuple<string, int>("Total users", users.Count),
                    new Tuple<string, int>("Amount of total users found with no passwords", invalids.Count),
                    new Tuple<string, int>("Amount of total users found with empty passwords", empty.Count),
                    new Tuple<string, int>("Amount of total users found with leaked passwords", leaks.Count),
                    new Tuple<string, int>("Amount of total users found with shared passwords", shared.Sum(s => s.Count())),
                    new Tuple<string, int>("Amount of total unique passwords shared", shared.Count())
                },
                new List<Tuple<string, int>>()
                {
                    new Tuple<string, int>("Total active users", users.Count(pred_enabled)),
                    new Tuple<string, int>("Amount of active users found with no passwords", invalids.Count(pred_enabled)),
                    new Tuple<string, int>("Amount of active users found with empty passwords", empty.Count(pred_enabled)),
                    new Tuple<string, int>("Amount of active users found with leaked passwords", leaks.Count(pred_enabled)),
                    new Tuple<string, int>("Amount of active users found with shared passwords", shared.Sum(s => s.Count(pred_enabled)))
                },
                new List<Tuple<string, int>>()
                {
                    new Tuple<string, int>("Total inactive users", users.Count(pred_disabled)),
                    new Tuple<string, int>("Amount of inactive users found with no passwords", invalids.Count(pred_disabled)),
                    new Tuple<string, int>("Amount of inactive users found with empty passwords", empty.Count(pred_disabled)),
                    new Tuple<string, int>("Amount of inactive users found with leaked passwords", leaks.Count(pred_disabled)),
                    new Tuple<string, int>("Amount of inactive users found with shared passwords", shared.Sum(s => s.Count(pred_disabled)))
                }
            };

            /* Write statistics to file */
            if (File.Exists(logfile))
                File.Delete(logfile);

            foreach (var stat in stats)
            {
                File.AppendAllLines(logfile, stat
                    .Select(s => string.Format("{0}: {1} ({2}%)", s.Item1, s.Item2, Math.Round(((double)s.Item2 / users.Count) * 100, 2)))
                    .ToArray());

                File.AppendAllText(logfile, Environment.NewLine);
            }

            if (empty.Count > 0)
            {
                File.AppendAllText(logfile, "Users with empty passwords:");
                File.AppendAllText(logfile, Environment.NewLine);

                File.AppendAllLines(logfile, empty
                    .Select(s => s.SamAccountName)
                    .ToArray());

                File.AppendAllText(logfile, Environment.NewLine);
            }

            if (leaks.Count > 0)
            {
                File.AppendAllText(logfile, "Users with leaked passwords:");
                File.AppendAllText(logfile, Environment.NewLine);

                File.AppendAllLines(logfile, leaks
                    .Select(s => string.Format("{0} ({1})", s.SamAccountName, s.Guid))
                    .ToArray());

                File.AppendAllText(logfile, Environment.NewLine);
            }

            if (shared.Count() > 0)
            {
                File.AppendAllText(logfile, "Users with shared passwords:");
                File.AppendAllText(logfile, Environment.NewLine);

                File.AppendAllLines(logfile, shared
                    .ToDictionary(k => k.Key, v => v.ToList())
                    .Select((s, i) => string.Format("{0}: {1}", i.ToString(), string.Join(",", s.Value.Select(u => u.SamAccountName))))
                    .ToArray());

                File.AppendAllText(logfile, Environment.NewLine);
            }

            if (invalids.Count > 0)
            {
                File.AppendAllText(logfile, "Users with no passwords:");
                File.AppendAllText(logfile, Environment.NewLine);

                File.AppendAllLines(logfile, invalids
                    .Select(s => s.SamAccountName)
                    .ToArray());
            }
        }
    }
}
