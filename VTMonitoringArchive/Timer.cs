using System;
using System.Timers;


namespace VTMonitoringArchive
{
    internal class Timer
    {
        static bool replicator = false;
        static bool violation = false;
        public static bool export = false;

        public static void OnViolationTimer(Object source, ElapsedEventArgs e)
        {
            foreach (string violation in Service.violations)
            {
                Service.Yesterday[violation.Replace(" ", "")] = SQL.Yesterday(violation.Replace(" ", ""));
            }
        }

        public static void OnStatusTimer(Object source, ElapsedEventArgs e) 
        {
            if (replicator && Convert.ToUInt32(Service.StatusJson["LastReplicationSeconds"]) > 10800)
            {
                Request.RebootHost("Replicator");
            }
            if (!replicator && Convert.ToUInt32(Service.StatusJson["LastReplicationSeconds"]) > 3600)
            {
                replicator = true;
                Request.ReStartService("VTTrafficReplicator");
            }

            if (violation && Convert.ToUInt32(Service.StatusJson["UnprocessedViolationsSeconds"]) > 10800)
            {
                Request.RebootHost("Violations");
            }
            if (!violation && Convert.ToUInt32(Service.StatusJson["UnprocessedViolationsSeconds"]) > 3600)
            {
                violation = true;
                Request.ReStartService("VTViolations");
            }

            if (export && Convert.ToUInt32(Service.StatusJson["UnexportedCount"]) != 0 && Convert.ToUInt32(Service.StatusJson["UnexportedSeconds"]) > 21600)
            {
                Request.RebootHost("Export");
            }
            if (!export && Convert.ToUInt32(Service.StatusJson["UnexportedSeconds"]) > 3600)
            {
                export = true;
                Request.ReStartService("VTTrafficExport");
            }
        } 

        public static void OnHostStatusTimer(Object source, ElapsedEventArgs e)
        {
            SQL.ResetUnprocessedViolations();

            if (SQL.CheckForPastAndFuture())
            {
//                SQL.RemovalOfPastAndFuture();
            }
            
            Service.StatusJson["UpTime"] = Request.GetUpTime().ToString();
            TimeSpan uptime = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["UpTime"]));
            Logs.WriteLine($"Host uptime {uptime}.");
            //-------------------------------------------------------------------------------------------------

            Service.StatusJson["DiskTotalSize"] = (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString().Replace(",", ".");
            Service.StatusJson["DiskTotalFreeSpace"] = (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString().Replace(",", ".");
            Service.StatusJson["DiskPercentSize"] = (Request.GetDiskUsagePercentage()).ToString().Replace(",", ".");
            Service.StatusJson["DiskPercentFreeSpace"] = (Request.GetDiskPercentFreeSpace()).ToString().Replace(",", ".");
            Logs.WriteLine($"Total disk size {Service.StatusJson["DiskTotalSize"]} GB, free space size {Service.StatusJson["DiskTotalFreeSpace"]} GB, disk size as a percentage {Service.StatusJson["DiskPercentSize"]}, free disk space percentage {Service.StatusJson["DiskPercentFreeSpace"]}.");
            //-------------------------------------------------------------------------------------------------

            string[] network = Request.GetNetwork();
            Service.StatusJson["NetworkNetspeed"] = network[0];
            Service.StatusJson["NetworkReceived"] = network[1];
            Service.StatusJson["NetworkSent"] = network[2];
            Logs.WriteLine($"Interface speed {Service.StatusJson["NetworkNetspeed"]}, incoming load {Service.StatusJson["NetworkReceived"]}, outgoing load {Service.StatusJson["NetworkSent"]}.");
            //-------------------------------------------------------------------------------------------------

            Service.StatusJson["ArchiveDepthSeconds"] = SQL.ArchiveDepthSeconds();
            Service.StatusJson["ArchiveDepthCount"] = SQL.ArchiveDepthCount();
            TimeSpan depthSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["ArchiveDepthSeconds"]));
            Logs.WriteLine($"Storage depth: time {depthSeconds}, number {Service.StatusJson["ArchiveDepthCount"]}.");
            //-------------------------------------------------------------------------------------------------

            UInt32 seconds = SQL.LastReplicationSeconds();
            Service.StatusJson["LastReplicationSeconds"] = seconds.ToString();
            TimeSpan replicationSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["LastReplicationSeconds"]));
            Logs.WriteLine($"Replication delay {replicationSeconds}.");
            //-------------------------------------------------------------------------------------------------

            UInt32 count = SQL.UnprocessedViolationsCount();
            UInt32 secondsViolarion = SQL.UnprocessedViolationsSeconds();

            Service.StatusJson["UnprocessedViolationsCount"] = count.ToString();
            Service.StatusJson["UnprocessedViolationsSeconds"] = secondsViolarion.ToString();
            TimeSpan timeViolationsSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["UnprocessedViolationsSeconds"]));
            Logs.WriteLine($"The delay in processing results is {timeViolationsSeconds}, in the amount of {count}.");
            //-------------------------------------------------------------------------------------------------

            UInt32 countexp = SQL.UnexportedCount();
            UInt32 secondsexp = SQL.UnexportedSeconds();

            Service.StatusJson["UnexportedCount"] = countexp.ToString();
            Service.StatusJson["UnexportedSeconds"] = secondsexp.ToString();
            TimeSpan timeExportSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["UnexportedSeconds"]));
            Logs.WriteLine($"The last violation was exported {timeExportSeconds} ago, leaving {countexp} to export.");
            //-------------------------------------------------------------------------------------------------

            Service.StatusJson["AllViolationsPrevioushour"] = SQL.AllViolationsPrevioushour();
            Logs.WriteLine($"All violations previoushour {Service.StatusJson["AllViolationsPrevioushour"]}.");
            //-------------------------------------------------------------------------------------------------

            Request.StatusNTPService();

            Logs.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
