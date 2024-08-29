using System;
using System.Data;
using System.Data.SqlClient;
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

        public static UInt32 UnprocessedViolationsSeconds()
        {
            string sqlQuery = "SELECT TOP(1) CHECKTIME FROM AVTO.dbo.CARS where PROCESSED = 0";
            object response = SQLQuery(sqlQuery) ?? 0;
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
                sb.Append(violation.Replace(" ", "") + " = 1 AND ");
            }
            string alarm = sb.ToString();

            string sqlQuery = $"SELECT COUNT_BIG(CARS_ID) FROM AVTO.dbo.CARS_VIOLATIONS WHERE {alarm} CHECKTIME > '{startDateTime:s}' AND CHECKTIME < '{endDateTime:s}'";
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

    }
}
