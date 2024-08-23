
using System.Timers;
using System;

namespace VTMonitoringArchive
{
    internal class Timer
    {
        public static void OnHostStatusTimer(Object source, ElapsedEventArgs e)
        {
            Service.StatusJson["UpTime"] = Request.GetUpTime().ToString();
            TimeSpan uptime = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["UpTime"]));
            Logs.WriteLine($"Host uptime in seconds {uptime}.");
            //-------------------------------------------------------------------------------------------------

            Service.StatusJson["DiskTotalSize"] = (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskTotalFreeSpace"] = (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskPercentSize"] = (Request.GetDiskUsagePercentage()).ToString();
            Service.StatusJson["DiskPercentFreeSpace"] = (Request.GetDiskPercentFreeSpace()).ToString();
            Logs.WriteLine($"Total disk size {Service.StatusJson["DiskTotalSize"]} GB, free space size {Service.StatusJson["DiskTotalFreeSpace"]} GB, disk size as a percentage {Service.StatusJson["DiskPercentSize"]}, free disk space percentage {Service.StatusJson["DiskPercentFreeSpace"]}.");
            //-------------------------------------------------------------------------------------------------

            Service.StatusJson["ArchiveDepthSeconds"] = SQL.ArchiveDepthSeconds();
            Service.StatusJson["ArchiveDepthCount"] = SQL.ArchiveDepthCount();
            TimeSpan depthSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["ArchiveDepthSeconds"]));
            Logs.WriteLine($"Storage depth: time {depthSeconds}, number {Service.StatusJson["ArchiveDepthCount"]}.");
            //-------------------------------------------------------------------------------------------------

            string[] network = Request.GetNetwork();
            Service.StatusJson["NetworkNetspeed"] = network[0];
            Service.StatusJson["NetworkReceived"] = network[1];
            Service.StatusJson["NetworkSent"] = network[2];
            Logs.WriteLine($"Interface speed {Service.StatusJson["NetworkNetspeed"]}, incoming load {Service.StatusJson["NetworkReceived"]}, outgoing load {Service.StatusJson["NetworkSent"]}.");
            //-------------------------------------------------------------------------------------------------


            Logs.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
