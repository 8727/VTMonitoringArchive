using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Collections;

namespace VTMonitoringArchive
{
    internal class Sorting
    {
        static Hashtable ViolationCode = new Hashtable();

        public static void HashVuolation()
        {
            ViolationCode.Add("0", "0 - Stream");
            ViolationCode.Add("2", "2 - OverSpeed");
            ViolationCode.Add("4", "4 - WrongDirection");
            ViolationCode.Add("5", "5 - BusLane");
            ViolationCode.Add("10", "10 - RedLightCross");
            ViolationCode.Add("31", "31 - SeatBelt");
            ViolationCode.Add("32", "32 - PhoneInHand");
            ViolationCode.Add("81", "81 - WrongCross");
            ViolationCode.Add("83", "83 - StopLine");
            ViolationCode.Add("90", "90 - WrongTurnTwoFrontier");
            ViolationCode.Add("112", "112 - WrongLineTurn");
            ViolationCode.Add("113", "113 - NoForwardZnak");
            ViolationCode.Add("114", "114 - NoUTurnOnlyForward");
            ViolationCode.Add("127", "127 - Lights");
            ViolationCode.Add("134", "134 - SeatBelt_Passanger");
        }

        public static void OnSortingTimer(Object source, ElapsedEventArgs e)
        {
            OnSorting();
        }

        public static void OnSorting()
        {
            if (!Service.sortingJob)
            {
                new Thread(() => { 
                    Logs.WriteLine($">>>>>>>> Sorting Start");
                    Service.sortingJob = true;
                    SortingFiles(Service.sourceFolderPr, Service.sortingFolderPr);
                    SortingFiles(Service.sourceFolderSc, Service.sortingFolderSc);
                    Service.sortingJob = false;
                    Logs.WriteLine($">>>>>>>> Sorting Stop");
                    Logs.WriteLine("-------------------------------------------------------------------------------");
                }).Start();
            }
        }

        static void SortingFiles(string sourcePath, string outPath)
        {
            if (Directory.Exists(sourcePath))
            {
                XmlDocument xFile = new XmlDocument();
                //string[] files = Directory.GetFiles(sourcePath, "*.xml", SearchOption.AllDirectories);
                string[] files = Directory.EnumerateFiles(sourcePath, "*.xml", SearchOption.AllDirectories).Take(Service.packageSize).ToArray();

                int countFiles = files.Length;

                if (countFiles > 0) { Timer.export = false; }
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    string PathSour = file.Remove(file.LastIndexOf("\\"));
                    string nameRemote = name.Remove(name.LastIndexOf("_"));

                    try
                    {
                        xFile.Load(file);

                        if (xFile.SelectSingleNode("//v_photo_ts") != null)
                        {
                            XmlNodeList violation_time = xFile.GetElementsByTagName("v_time_check");
                            string data = violation_time[0].InnerText.Remove(violation_time[0].InnerText.IndexOf("T"));
                            XmlNodeList violation_camera = xFile.GetElementsByTagName("v_camera");
                            XmlNodeList violation_pr_viol = xFile.GetElementsByTagName("v_pr_viol");

                            string Path = outPath + "\\" + data + "\\" + (string)ViolationCode[violation_pr_viol[0].InnerText] + "\\" + violation_camera[0].InnerText + "\\";

                            if (!(Directory.Exists(Path)))
                            {
                                Directory.CreateDirectory(Path);
                            }

                            if (Service.storageXML)
                            {
                                File.Copy(file, (Path + name), true);
                            }

                            if (Service.storageСollage && File.Exists(PathSour + "\\" + nameRemote + "_car.jpg"))
                            {
                                File.Copy((PathSour + "\\" + nameRemote + "_car.jpg"), (Path + nameRemote + "_car.jpg"), true);
                            }

                            if (Service.storageVideo && File.Exists(PathSour + "\\" + nameRemote + "__1video.mp4"))
                            {
                                File.Copy((PathSour + "\\" + nameRemote + "__1video.mp4"), (Path + nameRemote + "__1video.mp4"), true);
                            }

                            if (Service.storageVideo && File.Exists(PathSour + "\\" + nameRemote + "__2video.mp4"))
                            {
                                File.Copy((PathSour + "\\" + nameRemote + "__2video.mp4"), (Path + nameRemote + "__2video.mp4"), true);
                            }
                        }
                    }
                    catch 
                    {
                        
                        Logs.WriteLine($">>>>>>>> Broken file {name}");
                    }

                    string[] delFiles = Directory.GetFiles(sourcePath, (nameRemote + "*"), SearchOption.AllDirectories);
                    foreach (string delFile in delFiles)
                    {
                        File.Delete(delFile);
                    }
                }
                processDirectory(sourcePath);

                string[] delTimefiles = Directory.GetFiles(outPath, "*", SearchOption.AllDirectories);
                foreach (string delTimefile in delTimefiles)
                {
                    FileInfo fi = new FileInfo(delTimefile);
                    if (fi.CreationTime < DateTime.Now.AddDays(-Service.storageDays)) { fi.Delete(); }
                }
                processDirectory(outPath);

                Logs.WriteLine($">>>>>>>> Sorted {countFiles} violations");
            }
        }

        static void processDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
