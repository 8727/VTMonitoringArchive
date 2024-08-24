using System;
using Microsoft.Win32;
using System.Configuration;
using System.ServiceProcess;
using System.Collections;
using System.Linq;

namespace VTMonitoringArchive
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }
        public static TimeSpan localZone = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);

        public static Hashtable StatusJson = new Hashtable();
        public static Hashtable Yesterday = new Hashtable();

        public static string sourceFolderPr = "D:\\Duplo";
        public static string sourceFolderSc = "D:\\Doris";
        public static string sortingFolderPr = "D:\\!Duplo";
        public static string sortingFolderSc = "D:\\!Doris";

        public static bool sortingViolations = true;
        public static int storageDays = 35;
        public static int storageSortingIntervalMinutes = 20;
        public static bool storageXML = true;
        public static bool storageСollage = false;
        public static bool storageVideo = false;
        
        public static string networkMonitoring = "Ethernet";
        public static int dataUpdateInterval = 5;

        public static string monitoringOfUnloadings = "EXPORT2";

        public static string sqlSource = "(LOCAL)";
        public static string sqlUser = "sa";
        public static string sqlPassword = "1";

        public static string[] violations = { "ALARM_REDLIGHT", "ALARM_REDLIGHTCROSS", "ALARM_WRONG_LANE_TURN", "ALARM_WRONG_LEFT_TURN" };

        public static string diskMonitoring = "D:\\";
        public static bool statusWeb = true;

        void LoadConfig()
        {
            Logs.WriteLine("------------------------- Monitoring Service Settings -------------------------");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\VTMonitoringArchive", true))
            {
                if (key.GetValue("FailureActions") == null)
                {
                    key.SetValue("FailureActions", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00 });
                }
            }

            if (ConfigurationManager.AppSettings.Count != 0)
            {
                sourceFolderPr = ConfigurationManager.AppSettings["SourceFolderPr"];
                sortingFolderPr = ConfigurationManager.AppSettings["SortingFolderPr"];

                sourceFolderSc = ConfigurationManager.AppSettings["SourceFolderSc"];
                sortingFolderSc = ConfigurationManager.AppSettings["SortingFolderSc"];

                sortingViolations = Convert.ToBoolean(ConfigurationManager.AppSettings["SortingViolations"]);
                storageDays = Convert.ToInt32(ConfigurationManager.AppSettings["StorageDays"]);
                storageSortingIntervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["SortingIntervalMinutes"]);
                storageXML = Convert.ToBoolean(ConfigurationManager.AppSettings["StorageXML"]);
                storageСollage = Convert.ToBoolean(ConfigurationManager.AppSettings["StorageСollage"]);
                storageVideo = Convert.ToBoolean(ConfigurationManager.AppSettings["StorageVideo"]);

                networkMonitoring = ConfigurationManager.AppSettings["NetworkMonitoring"];
                dataUpdateInterval = Convert.ToInt32(ConfigurationManager.AppSettings["DataUpdateIntervalMinutes"]);

                monitoringOfUnloadings = ConfigurationManager.AppSettings["MonitoringOfUnloadings"];

                sqlSource = ConfigurationManager.AppSettings["SQLDataSource"];
                sqlUser = ConfigurationManager.AppSettings["SQLUser"];
                sqlPassword = ConfigurationManager.AppSettings["SQLPassword"];

                violations = ConfigurationManager.AppSettings["violations"].Split(',').Select(n => Convert.ToString(n)).ToArray();
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Vocord\VTScrshtDB"))
            {
                if (key != null)
                {
                    if (key.GetValue("WorkingDirectories") != null)
                    {
                        diskMonitoring = key.GetValue("WorkingDirectories").ToString();
                    }
                }
            }

            if (sortingViolations)
            {
                var storageTimer = new System.Timers.Timer(storageSortingIntervalMinutes * 60000);
                storageTimer.Elapsed += Sorting.OnSortingTimer;
                storageTimer.AutoReset = true;
                storageTimer.Enabled = true;
                Logs.WriteLine($">>>>> Violation sorting is enabled at {storageSortingIntervalMinutes} minute intervals.");
            }

            var hostStatusTimer = new System.Timers.Timer(dataUpdateInterval * 60000);
            hostStatusTimer.Elapsed += Timer.OnHostStatusTimer;
            hostStatusTimer.AutoReset = true;
            hostStatusTimer.Enabled = true;

            var statusTimer = new System.Timers.Timer(10 * 60000);
            statusTimer.Elapsed += Timer.OnStatusTimer;
            statusTimer.AutoReset = true;
            statusTimer.Enabled = true;

            var violationTimer = new System.Timers.Timer(60 * 60000);
            violationTimer.Elapsed += Timer.OnViolationTimer;
            violationTimer.AutoReset = true;
            violationTimer.Enabled = true;

            Logs.WriteLine($">>>>> Monitoring host parameters at {dataUpdateInterval} minute intervals.");
            Logs.WriteLine("-------------------------------------------------------------------------------");
        }

        void CreatedStatusJson()
        {
            StatusJson.Add("UpTime", Request.GetUpTime().ToString());

            StatusJson.Add("DiskTotalSize", (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskTotalFreeSpace", (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskPercentTotalSize", Request.GetDiskUsagePercentage().ToString());
            StatusJson.Add("DiskPercentTotalFreeSpace", Request.GetDiskPercentFreeSpace().ToString());

            StatusJson.Add("ArchiveDepthSeconds", SQL.ArchiveDepthSeconds());
            StatusJson.Add("ArchiveDepthCount", SQL.ArchiveDepthCount());

            string[] network = Request.GetNetwork();
            StatusJson.Add("NetworkNetspeed", network[0]);
            StatusJson.Add("NetworkReceived", network[1]);
            StatusJson.Add("NetworkSent", network[2]);

            StatusJson.Add("LastReplicationSeconds", SQL.LastReplicationSeconds().ToString());

            StatusJson.Add("UnprocessedViolationsCount", SQL.UnprocessedViolationsCount().ToString());
            StatusJson.Add("UnprocessedViolationsSeconds", SQL.UnprocessedViolationsSeconds().ToString());

            StatusJson.Add("UnexportedCount", SQL.UnexportedCount().ToString());
            StatusJson.Add("UnexportedSeconds", SQL.UnexportedSeconds().ToString());

            StatusJson.Add("AllViolationsPrevioushour", SQL.AllViolationsPrevioushour());

            foreach (string violation in violations)
            {
                Yesterday.Add(violation.Replace(" ", ""), SQL.Yesterday(violation.Replace(" ", "")));
            }
        }

        protected override void OnStart(string[] args)
        {
            Logs.WriteLine("*******************************************************************************");
            Logs.WriteLine("************************** Service Monitoring START ***************************");
            Logs.WriteLine("*******************************************************************************");
            LoadConfig();
            Sorting.HashVuolation();
            CreatedStatusJson();
            Web.WEBServer.Start();
        }

        protected override void OnStop()
        {
            statusWeb = false;
            Web.WEBServer.Interrupt();
            Logs.WriteLine("*******************************************************************************");
            Logs.WriteLine("*************************** Service Monitoring STOP ***************************");
            Logs.WriteLine("*******************************************************************************");
        }
    }
}
