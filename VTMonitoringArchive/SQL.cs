using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace VTMonitoringArchive
{
    internal class SQL
    {
        static string connectionString = $@"Data Source={Service.sqlSource};Initial Catalog=AVTO;User Id={Service.sqlUser};Password={Service.sqlPassword};Connection Timeout=60";

        static object SQLQuery(string query)
        {
            object response = -1;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    response = command.ExecuteScalar();
                }
                catch (SqlException)
                {
                    connection.Close();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
            return response;
        }

        static UInt32 DateTimeToSecondes(string dt)
        {
            if (dt == "-1" )
            {
                dt = "01.01.2000 00:00:00";
            }
            if (dt == "-2")
            {
                dt = DateTime.UtcNow.ToString("d.M.yyyy HH:mm:ss");
            }

            DateTime converDateTime = DateTime.ParseExact(dt, "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(+Service.localZone);
            return Convert.ToUInt32(DateTime.Now.Subtract(converDateTime).TotalSeconds);
        }

        public static UInt32 LastReplicationSeconds()
        {
            string sqlQuery = "SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS ORDER BY CARS_ID DESC";
            object response = SQLQuery(sqlQuery) ?? "-2";
            return DateTimeToSecondes(response.ToString());
        }

        public static UInt32 UnprocessedViolationsCount()
        {
            string sqlQuery = "SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS where PROCESSED = 0";
            return Convert.ToUInt32(SQLQuery(sqlQuery));
        }

        public static void ResetUnprocessedViolations()
        {
            string sqlQuery = "UPDATE dbo.CARS SET PROCESSED = 1 WHERE PROCESSED = 0 AND DETECTEDGRN = ''";
            _ = SQLQuery(sqlQuery);
            
        }

        public static UInt32 UnprocessedViolationsSeconds()
        {
            string sqlQuery = "SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS where PROCESSED = 0";
            object response = SQLQuery(sqlQuery) ?? "-2";
            return DateTimeToSecondes(response.ToString());
        }

        public static UInt32 UnexportedCount()
        {
            string sqlQuery = $"SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS_VIOLATIONS where {Service.monitoringOfUnloadings} = 0";
            return Convert.ToUInt32(SQLQuery(sqlQuery));
        }

        public static UInt32 UnexportedSeconds()
        {
            string sqlQuery = $"SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS_VIOLATIONS where {Service.monitoringOfUnloadings} = 1 ORDER BY CARS_ID DESC";
            object response = SQLQuery(sqlQuery) ?? "-2";
            return DateTimeToSecondes(response.ToString());
        }

        public static string ArchiveDepthSeconds()
        {
            string oldEntry = "SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS";
            string lastEntry = "SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS ORDER BY CARS_ID DESC";

            DateTime archiveOld = DateTime.ParseExact(SQLQuery(oldEntry).ToString(), "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(+Service.localZone);
            DateTime archiveLast = DateTime.ParseExact(SQLQuery(lastEntry).ToString(), "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(+Service.localZone);

            return archiveLast.Subtract(archiveOld).TotalSeconds.ToString();
        }

        public static string ArchiveDepthCount()
        {
            string sqlQuery = "SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS";
            return SQLQuery(sqlQuery).ToString();
        }

        public static string AllViolationsPrevioushour()
        {
            DateTime endDateTime = DateTime.UtcNow;
            DateTime startDateTime = endDateTime.AddHours(-1);

            StringBuilder sb = new StringBuilder(Service.violations.Length);
            foreach (string violation in Service.violations)
            {
                sb.Append(violation.Replace(" ", "") + " = 1 OR ");
            }
            string alarm = sb.ToString().Remove(sb.Length - 4);

            string sqlQuery = $"SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS_VIOLATIONS WHERE CHECKTIME > '{startDateTime:s}' AND CHECKTIME < '{endDateTime:s}' AND ({alarm})";
            return SQLQuery(sqlQuery).ToString();
        }

        public static string Yesterday(string alarm)
        {
            String getDateTime = DateTime.Now.ToString("d.M.yyyy 00:00:00");
            DateTime endDateTime = DateTime.ParseExact(getDateTime, "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(-Service.localZone);
            DateTime startDateTime = endDateTime.AddDays(-1);

            string sqlQuery = $"SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS_VIOLATIONS WHERE {alarm} = 1 AND CHECKTIME > '{startDateTime:s}' AND CHECKTIME < '{endDateTime:s}'";
            return SQLQuery(sqlQuery).ToString();
        }

        public static bool CheckForPastAndFuture()
        {
            bool status = false;
            String getDateTime = DateTime.Now.AddDays(+1).ToString("d.M.yyyy 00:00:00");
            DateTime endDateTime = DateTime.ParseExact(getDateTime, "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(-Service.localZone);
            DateTime startDateTime = endDateTime.AddDays(-365);

            string sqlSelectInformation = $"SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS_INFORMATION where CHECKTIME < '{startDateTime:s}' OR  CHECKTIME > '{endDateTime:s}'";

            if (SQLQuery(sqlSelectInformation).ToString() != "0")
            {
                status = true;
            }

            return status;
        }

        public static void RemovalOfPastAndFuture()
        {
            String getDateTime = DateTime.Now.AddDays(+1).ToString("d.M.yyyy 00:00:00");
            DateTime endDateTime = DateTime.ParseExact(getDateTime, "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Add(-Service.localZone);
            DateTime startDateTime = endDateTime.AddDays(-365);

            string sqlDeleteCars = $"DELETE FROM AVTO.dbo.CARS where CHECKTIME < '{startDateTime:s}' OR  CHECKTIME > '{endDateTime:s}'";
            string sqlDeleteViolations = $"DELETE FROM AVTO.dbo.CARS_VIOLATIONS where CHECKTIME < '{startDateTime:s}' OR  CHECKTIME > '{endDateTime:s}'";
            string sqlDeletePartitions = $"DELETE FROM AVTO.dbo.PARTITIONS_NOW where DATEFROM < '{startDateTime:s}' OR  DATEFROM > '{endDateTime:s}'";
            string sqlUpdateCars = $"UPDATE AVTO.dbo.CARS SET PROCESSED = NULL WHERE PROCESSED = 0";
            string sqlSelectInformation = $"SELECT SCREENSHOT FROM AVTO.dbo.CARS_INFORMATION where CHECKTIME < '{startDateTime:s}' OR  CHECKTIME > '{endDateTime:s}'";
            string sqlDeleteInformation = $"DELETE FROM AVTO.dbo.CARS_INFORMATION where CHECKTIME < '{startDateTime:s}' OR CHECKTIME > '{endDateTime:s}'";

            SQLQuery(sqlDeleteCars);
            SQLQuery(sqlDeleteViolations);
            SQLQuery(sqlDeletePartitions);
            SQLQuery(sqlUpdateCars);
            SqlDataReader RemovingFiles = (SqlDataReader)SQLQuery(sqlSelectInformation);
            if (RemovingFiles.HasRows)
            {
                while (RemovingFiles.Read())
                {
                    try
                    {
                        Directory.Delete(RemovingFiles.GetString(0), true);
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                }
            }

            SQLQuery(sqlDeleteInformation);
        }
    }
}
