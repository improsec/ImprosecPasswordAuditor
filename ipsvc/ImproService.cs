using DSInternals.Common.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace ImproService
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    public partial class ImproService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        static int TickCounter { get; set; } = 0;
        static DateTime? LastStatistic { get; set; } = null;

        static string last_ip = string.Empty;
        static string last_server = string.Empty;

        public ImproService(string[] args)
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SERVICE_STATUS status = new SERVICE_STATUS();
            status.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            status.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref status);

            Init(args);

            status.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref status);
        }

        protected override void OnStop()
        {
            SERVICE_STATUS status = new SERVICE_STATUS();
            status.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            status.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref status);

            Uninit();

            status.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref status);
        }

        public void Init(string[] args)
        {
            Config.LoadSettings();
            Scanner.LoadAccountStates();

            Logging.WriteLog("Application", "Starting blacklisting service");

            Timer stat_timer = new Timer { Interval = 300000 };
            stat_timer.Elapsed += new ElapsedEventHandler(this.OnStatistics);
            stat_timer.Start();

            Timer scan_timer = new Timer { Interval = double.Parse(Config.Read("tickdelay")) };
            scan_timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            scan_timer.Start();
        }
        
        public void Uninit()
        {
            Logging.WriteLog("Application", "Stopping blacklisting service");

            Scanner.SaveAccountStates();
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            string tick = string.Format("Tick #{0}", TickCounter++);

            try
            {
                string server = Server.FindDomainController(Config.Read("domain"), ref last_ip, ref last_server);

                if (server == null)
                    Logging.WriteLog(tick, "Could not find a live domain controller on the selected domain", EventLogEntryType.Error);
                else
                {
                    Logging.WriteLog(tick, "Reading all users from the selected AD domain");
                    var users = Scanner.ReadAllUsers(server, Config.Read("context"));

                    if (users.Count == 0)
                        Logging.WriteLog(tick, "AD replication returned no users. Please verify that the configuration is correct.", EventLogEntryType.Warning);
                    else
                    {
                        Logging.WriteLog(tick, "Checking for updated users since last check");
                        var update = Scanner.UpdateAccounts(users);

                        Logging.WriteLog(tick, "Scanning for leaked passwords");
                        var leaks = Scanner.ScanUsers(update);

                        Logging.WriteLog(tick, "Reporting results to event log");

                        foreach (var u in update)
                        {
                            if (u.NTHash == null)
                            {
                                if (u.Enabled)
                                    Logging.WriteEventLog(string.Format("No password found for active user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Error, 100);
                                else if (Config.ScanInactive)
                                    Logging.WriteEventLog(string.Format("No password found for inactive user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Warning, 101);
                            }
                            else if (Utility.CompareEmpty(u.NTHash))
                            {
                                if (u.Enabled)
                                    Logging.WriteEventLog(string.Format("Empty password found for active user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Error, 102);
                                else if (Config.ScanInactive)
                                    Logging.WriteEventLog(string.Format("Empty password found for inactive user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Warning, 103);
                            }
                        }

                        foreach (var u in leaks)
                        {
                            if (u.Enabled)
                                Logging.WriteEventLog(string.Format("Leaked password found for active user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Error, 104);
                            else if (Config.ScanInactive)
                                Logging.WriteEventLog(string.Format("Leaked password found for inactive user {0} ({1})", u.Guid, u.SamAccountName), EventLogEntryType.Warning, 105);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog(tick, e.ToString(), EventLogEntryType.Error);
            }
        }

        private void OnStatistics(object sender, ElapsedEventArgs args)
        {
            var schedule = DateTime.Today.AddHours(3); // Run at 03:00 (3 AM)

            if (DateTime.Now > schedule && (!LastStatistic.HasValue || schedule > LastStatistic.Value))
            {
                try
                {
                    LastStatistic = DateTime.Now;

                    string server = Server.FindDomainController(Config.Read("domain"), ref last_ip, ref last_server);

                    if (server == null)
                        Logging.WriteLog("Statistics", "Could not find a live domain controller on the selected domain", EventLogEntryType.Error);
                    else
                    {
                        Logging.WriteLog("Statistics", "Reading all users from the selected AD domain");
                        var users = Scanner.ReadAllUsers(server, Config.Read("context"));

                        if (users.Count == 0)
                            Logging.WriteLog("Statistics", "AD replication returned no users. Please verify that the configuration is correct.", EventLogEntryType.Warning);
                        else
                        {
                            Logging.WriteLog("Statistics", "Scanning for leaked passwords");
                            var leaks = Scanner.ScanUsers(users);

                            Logging.WriteLog("Statistics", "Writing results to statistics log");
                            Statistics.CreateStatistics(users, leaks);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.WriteLog("Statistics", e.ToString(), EventLogEntryType.Error);
                }
            }
        }
    }
}
