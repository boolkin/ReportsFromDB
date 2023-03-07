using System;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace ReportDB
{
    class Program
    {
        static List<string[]> SQLrows = new List<string[]>();
        static int columns;
        static void Main(string[] args)
        {
            try
            {
                // Получаем данные, необходимые для соединения
                // Создаем поток для прослушивания
                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
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
                string responseString = "";
                // текущий каталог исполняемого файла
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var path = System.IO.Path.GetDirectoryName(strExeFilePath);
                try
                {
                    string[] req = request.RawUrl.Split(';');
                    string reply = "";
                    if (req[3] == "clear")
                    {
                        SQLrows.Clear();
                    }
                    reply = connectToDb(req[1], req[2]);
                    if (reply == "ok" || reply == "full")
                    {
                        responseString = buildTable();
                    }

                    else
                    {
                        responseString = reply;
                    }
                }
                catch
                {
                    try
                    {
                        responseString = System.IO.File.ReadAllText(System.IO.Path.Join(path, "/Resources/index.html"));
                    }
                    catch
                    {
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

        static string buildTable()
        {
            Decimal pages = Math.Floor((Decimal)SQLrows.Count / 100) + 1;
            string htmlBlank = "<!doctype html><html lang='ru'><head><meta charset='utf-8'/><style>td{width:20%;}</style></head><body><h2 align='center'>Отчет по простоям (" + pages + " стр.)</h2>";
            string table = "<table border='1' rules='all' align='center'><tr><th>Начало простоя</th><th>Конец простоя</th><th>Продолжительность</th></tr>";
            string allrows = " ";
            int i = 0;
            while (SQLrows.Count > 0)
            {
                string row = "<tr>";
                for (int j = 0; j < columns; j++)
                {
                    row += $"<td>{SQLrows[0][j]}</td>\n";
                }
                row += "</tr>";
                allrows += row;
                SQLrows.RemoveAt(0);
                i++;
                if (i == 100) break;
            }
            return htmlBlank + table + allrows + "</table></body></html>";
        }
        static string connectToDb(string dateFrom, string dateTill)
        {

            var connectionStringBuilder = new SqliteConnectionStringBuilder();
            // текущий каталог исполняемого файла
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = System.IO.Path.GetDirectoryName(strExeFilePath);
            string folder = System.IO.Path.Join(path, "/Resources/");
            System.IO.Directory.CreateDirectory(folder);
            //Use DB in project directory.  If it does not exist, create it:
            connectionStringBuilder.DataSource = System.IO.Path.Join(folder, "My.db");
            if (SQLrows.Count > 0) return "full";
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
                        selectCmd.CommandText = $"SELECT * FROM CGL_down WHERE Downtime_start > '{dateFrom}' and Downtime_stop < date('{dateTill}','+1 day') ORDER BY Downtime_start ";

                        using (var showTbl = selectCmd.ExecuteReader())
                        {
                            while (showTbl.Read())
                            {
                                string[] strArr = new string[showTbl.FieldCount];
                                for (int i = 0; i < strArr.Length; i++)
                                {
                                    strArr[i] = showTbl.GetString(i);
                                }
                                SQLrows.Add(strArr);
                            }
                            columns = showTbl.FieldCount;
                            connection.Close();
                            return "ok";
                        }
                    }
                    else
                    {
                        return "Отсутствует БД";
                    }
                }

            }
        }
        public static void Receiver()
        {
            int localPort = 3310;
            // Создаем UdpClient для чтения входящих данных
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                while (true)
                {
                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);

                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    //Console.WriteLine(" -> " + returnData.ToString());
                    // можно убрать комментирование и тогда принятые данные будут показываться текстом в окне консоли
                    try
                    {
                        var connectionStringBuilder = new SqliteConnectionStringBuilder();
                        // текущий каталог исполняемого файла
                        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        var path = System.IO.Path.GetDirectoryName(strExeFilePath);
                        string folder = System.IO.Path.Join(path, "/Resources/");
                        System.IO.Directory.CreateDirectory(folder);
                        //Use DB in project directory.  If it does not exist, create it:
                        connectionStringBuilder.DataSource = System.IO.Path.Join(folder, "My.db");
                        using (var connection = new SqliteConnection(connectionStringBuilder.ConnectionString))
                        {
                            connection.Open();
                            var insertToTbl = connection.CreateCommand();
                            byte[] byteArray = receiveBytes;
                            insertToTbl.CommandText = "CREATE TABLE if not exists raw_data (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, data BLOB)";
                            insertToTbl.ExecuteNonQuery();
                            insertToTbl.CommandText = "INSERT INTO raw_data (data) VALUES (@byteArray)";
                            insertToTbl.Parameters.AddWithValue("@byteArray", byteArray);
                            insertToTbl.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("SQL исключение: " + ex.ToString() + "\n  " + ex.Message);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
    }
}