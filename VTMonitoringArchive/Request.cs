using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net.NetworkInformation;


namespace VTMonitoringArchive
{
    internal class Request
    {
        static DriveInfo driveInfo = new DriveInfo(Service.diskMonitoring);

        public static byte GetPing(string ip)
        {
            byte result = 0;
            PingReply p = new Ping().Send(ip, 5000);
            if (p.Status == IPStatus.Success)
            {
                result = 1;
            }
            return result;
        }

        public static UInt32 GetUpTime()
        {
            PerformanceCounter uptime = new PerformanceCounter("System", "System Up Time");
            uptime.NextValue();
            return Convert.ToUInt32(uptime.NextValue());
        }

        public static long GetDiskTotalSize()
        {
            return driveInfo.TotalSize;
        }

        public static long GetDiskTotalFreeSpace()
        {
            return driveInfo.TotalFreeSpace; ;
        }

        public static double GetDiskUsagePercentage()
        {
            return (driveInfo.TotalFreeSpace / (driveInfo.TotalSize / 100.0));
        }

        public static double GetDiskPercentFreeSpace()
        {
            return (100 - (driveInfo.TotalFreeSpace / (driveInfo.TotalSize / 100.0)));
        }

        public static string[] GetNetwork()
        {
            long oldReceived = 0;
            long oldSent = 0;
            long lastReceived = 0;
            long lastSent = 0;
            UInt16 speed = 0;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters.Where(a => a.Name == Service.networkMonitoring))
            {
                var ipv4Info = adapter.GetIPv4Statistics();
                oldReceived = ipv4Info.BytesReceived;
                oldSent = ipv4Info.BytesSent;
            }
            Thread.Sleep(1000);
            foreach (NetworkInterface adapter in adapters.Where(a => a.Name == Service.networkMonitoring))
            {
                var ipv4Info = adapter.GetIPv4Statistics();
                lastReceived = ipv4Info.BytesReceived;
                lastSent = ipv4Info.BytesSent;
                speed = Convert.ToUInt16(adapter.Speed / 1000000);
            }
            string[] req = { speed.ToString(), ((lastReceived - oldReceived) / 131072.0).ToString(), ((lastSent - oldSent) / 131072.0).ToString() };
            return req;
        }


    }
}
