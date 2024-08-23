using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;


namespace VTMonitoringArchive
{
    internal class Web
    {
        static HttpListener serverWeb;
        public static Thread WEBServer = new Thread(ThreadWEBServer);

        static void ThreadWEBServer()
        {
            serverWeb = new HttpListener();
            serverWeb.Prefixes.Add(@"http://+:8030/");
            serverWeb.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            serverWeb.Start();
            while (Service.statusWeb)
            {
                ProcessRequest();
            }
        }

        static void ProcessRequest()
        {
            var result = serverWeb.BeginGetContext(ListenerCallback, serverWeb);
            var startNew = Stopwatch.StartNew();
            result.AsyncWaitHandle.WaitOne();
            startNew.Stop();
        }

        static void ListenerCallback(IAsyncResult result)
        {
            var HttpResponse = serverWeb.EndGetContext(result);
            //string key = HttpResponse.Request.QueryString["key"];
            string json = "{\n\t\"dateTime\":\"" + DateTime.Now.ToString() + "\"";

            json += ",\n\t\"upTime\":\"" + Service.StatusJson["UpTime"] + "\"";

            json += ",\n\t\"diskTotalSize\":\"" + Service.StatusJson["DiskTotalSize"] + "\"";
            json += ",\n\t\"diskTotalFreeSpace\":\"" + Service.StatusJson["DiskTotalFreeSpace"] + "\"";
            json += ",\n\t\"diskPercentTotalSize\":\"" + Service.StatusJson["DiskPercentTotalSize"] + "\"";
            json += ",\n\t\"diskPercentTotalFreeSpace\":\"" + Service.StatusJson["DiskPercentTotalFreeSpace"] + "\"";

            json += ",\n\t\"archiveDepthSeconds\":\"" + Service.StatusJson["ArchiveDepthSeconds"] + "\"";
            json += ",\n\t\"archiveDepthCount\":\"" + Service.StatusJson["ArchiveDepthCount"] + "\"";

            json += ",\n\t\"networkNetspeed\":\"" + Service.StatusJson["NetworkNetspeed"] + "\"";
            json += ",\n\t\"networkSent\":\"" + Service.StatusJson["NetworkSent"] + "\"";
            json += ",\n\t\"networkReceived\":\"" + Service.StatusJson["NetworkReceived"] + "\"";


            json += "\n}";

            HttpResponse.Response.Headers.Add("Content-Type", "application/json");
            HttpResponse.Response.StatusCode = 200;
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            HttpResponse.Response.ContentLength64 = buffer.Length;
            HttpResponse.Response.OutputStream.Write(buffer, 0, buffer.Length);
            HttpResponse.Response.Close();
        }
    }
}
