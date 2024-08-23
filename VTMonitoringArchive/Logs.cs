using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


namespace VTMonitoringArchive
{
    internal class Logs
    {
        static int logFileName = 0;

        static public void WriteLine(string message)
        {
            if (!(Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log")))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log");
            }

            string logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log";

            string[] tempfiles = Directory.GetFiles(logDir, "-log.txt", SearchOption.AllDirectories);

            if (tempfiles.Count() != 0)
            {
                foreach (string file in tempfiles)
                {
                    string names = Path.GetFileName(file);
                    Regex regex = new Regex(@"\d{4}-");
                    if (regex.IsMatch(names))
                    {
                        int number = (int.Parse(names.Remove(names.IndexOf("-"))));
                        if (number > logFileName)
                        {
                            logFileName = number;
                        }
                    }
                }
            }

            string name = logFileName.ToString("0000");
            FileInfo fileInfo = new FileInfo(logDir + $"\\{name}-log.txt");
            using (StreamWriter sw = fileInfo.AppendText())
            {
                sw.WriteLine(String.Format("{0:yyMMdd hh:mm:ss} {1}", DateTime.Now.ToString(), message));
                sw.Close();
                if (fileInfo.Length > 204800)
                {
                    logFileName++;
                }

                string[] delTimefiles = Directory.GetFiles(logDir, "*", SearchOption.AllDirectories);
                foreach (string delTimefile in delTimefiles)
                {
                    FileInfo fi = new FileInfo(delTimefile);
                    if (fi.CreationTime < DateTime.Now.AddDays(-Service.storageDays)) { fi.Delete(); }
                }
            }
        }
    }
}
