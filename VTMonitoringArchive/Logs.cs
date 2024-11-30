using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


namespace VTMonitoringArchive
{
    internal class Logs
    {
        static public void WriteLine(string message)
        {
            if (!(Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs")))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs");
            }

            string logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs";

            String[] files = Directory.GetFiles(logDir).OrderByDescending(d => new FileInfo(d).CreationTime).ToArray();

            string file = logDir + "\\000000.log";
            if (files.Length > 0)
            {
                file = files[0];
            }

            FileInfo fileInfo = new FileInfo(file);
            if (fileInfo.Exists)
            {
                if (fileInfo.Length > 204800)
                {
                    string names = Path.GetFileName(file);
                    Regex regex = new Regex(@"\d{6}");
                    if (regex.IsMatch(names))
                    {
                        int number = (int.Parse(names.Remove(names.IndexOf("."))));
                        number++;
                        string name = number.ToString("000000");
                        file = logDir + $"\\{name}.log";
                    }
                }
            }
            fileInfo = new FileInfo(file);
            using (StreamWriter sw = fileInfo.AppendText())
            {
                sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"), message));
                sw.Close();

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
