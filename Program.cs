using System;
using Microsoft.Data.Sqlite;
using System.Net;

namespace ReportDB
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateListener();
        }

        public static void CreateListener()
        {
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            string url = "http://*";
            string port = "8880";
            string prefix = String.Format("{0}:{1}/", url, port);
            listener.Prefixes.Add(prefix);
            listener.Start();
            Console.WriteLine("Listening...");
            while (true)
            {

                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                string responseString;
                try
                {
                    string[] subs = request.RawUrl.Split(';');
                    string htmlBlank = "<!doctype html><html lang='ru'><head><meta charset='utf-8'/><title>Отчет</title><style>td{width:20%;}</style></head><body>";
                    responseString = htmlBlank + connectToDb(subs[1], subs[2]) + "</body></html>";
                }
                catch
                {
                    // текущий каталог исполняемого файла
                    string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var path = System.IO.Path.GetDirectoryName(strExeFilePath);
                    // Read the file as one string.
                    try {
                        string textHTML = System.IO.File.ReadAllText(System.IO.Path.Join(path, "index.html"));
                        responseString = textHTML;
                    }
                    catch{
                        responseString = "add index.html to generate page";
                    }
                }



                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.

            }
        }
        static string connectToDb(string dateFrom, string dateTill)
        {
            //Console.WriteLine($"start {dateFrom}, end {dateTill}");
            var connectionStringBuilder = new SqliteConnectionStringBuilder();
            // текущий каталог исполняемого файла
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = System.IO.Path.GetDirectoryName(strExeFilePath);
            string folder = System.IO.Path.Join(path, "/DataBases/");
            System.IO.Directory.CreateDirectory(folder);
            //Use DB in project directory.  If it does not exist, create it:
            connectionStringBuilder.DataSource = System.IO.Path.Join(folder, "My.db");

            using (var connection = new SqliteConnection(connectionStringBuilder.ConnectionString))
            {
                connection.Open();

                var checkTbl = connection.CreateCommand();
                checkTbl.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='CGL_down';";

                using (var reader = checkTbl.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        var selectCmd = connection.CreateCommand();
                        selectCmd.CommandText = $"SELECT * FROM CGL_down WHERE Downtime_start > '{dateFrom}' and Downtime_stop < date('{dateTill}','+1 day')";

                        string result = "<table border='1'><tr><th>Начало простоя</th><th>Конец простоя</th><th>Продолжительность</th></tr>";
                        using (var showTbl = selectCmd.ExecuteReader())
                        {
                            while (showTbl.Read())
                            {
                                result = result + $"<tr><td>{showTbl.GetString(0)}</td><td>{showTbl.GetString(1)}</td><td>{showTbl.GetString(2)}</td></tr>";

                            }
                            result += "</table>";
                            connection.Close();
                            return result;
                        }
                    }
                    else
                    {
                        var createTableCmd = connection.CreateCommand();
                        createTableCmd.CommandText = "CREATE TABLE if not exists CGL_down (Downtime_start TEXT NOT NULL,Downtime_stop  TEXT NOT NULL, Downtime_dur   TEXT NOT NULL)";
                        createTableCmd.ExecuteNonQuery();

                        //Seed some data:
                        using (var transaction = connection.BeginTransaction())
                        {
                            var insertCmd = connection.CreateCommand();
                            insertCmd.CommandText = "INSERT INTO CGL_down VALUES('2023-01-28 10:00:00.000','2023-01-28 10:15:00.000','00:15:00.000')";
                            insertCmd.ExecuteNonQuery();
                            insertCmd.CommandText = "INSERT INTO CGL_down VALUES('2023-01-29 13:00:00.000','2023-01-29 13:10:00.000','00:10:00.000')";
                            insertCmd.ExecuteNonQuery();
                            insertCmd.CommandText = "INSERT INTO CGL_down VALUES('2023-01-30 20:30:00.000','2023-01-30 20:52:00.000','00:22:00.000')";
                            insertCmd.ExecuteNonQuery();
                            transaction.Commit();
                        }
                        connection.Close();
                        return "Создана новая БД";
                    }
                }

            }
        }
    }
}