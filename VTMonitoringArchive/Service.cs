using System;
using Microsoft.Win32;
using System.Configuration;
using System.ServiceProcess;
using System.Collections;

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

        public static int dataUpdateInterval = 5;
        
        public static string networkMonitoring = "vEthernet (LAN)";
        public static string diskMonitoring = "D:\\";

        public static string sqlSource = "(LOCAL)";
        public static string sqlUser = "sa";
        public static string sqlPassword = "1";

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

                sqlSource = ConfigurationManager.AppSettings["SQLDataSource"];
                sqlUser = ConfigurationManager.AppSettings["SQLUser"];
                sqlPassword = ConfigurationManager.AppSettings["SQLPassword"];
            }

            var hostStatusTimer = new System.Timers.Timer(dataUpdateInterval * 60000);
            hostStatusTimer.Elapsed += Timer.OnHostStatusTimer;
            hostStatusTimer.AutoReset = true;
            hostStatusTimer.Enabled = true;


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

        }

        protected override void OnStart(string[] args)
        {
            Logs.WriteLine("*******************************************************************************");
            Logs.WriteLine("************************** Service Monitoring START ***************************");
            Logs.WriteLine("*******************************************************************************");
            LoadConfig();
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
