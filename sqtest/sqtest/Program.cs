using System.Data.SQLite;
using System.Data.Common;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Text;
using System;
using System.Threading;
using System.Security.Cryptography;

namespace sqtest
{
    /// <summary>
    /// Класс, содержащий e-mail пользователя и пароль к нему
    /// </summary>
    class User
    {
        public string MailAddress;
        public string password;
        public User(string MailAddress, string password)
        {
            this.MailAddress = MailAddress;
            this.password = password;
        }
    }
    /// <summary>
    /// Класс, содержащий пользователя-отправителя, e-mail получателя,
    /// тему сообщения, текст сообщения, время отправки сообщения
    /// </summary>
    class Message
    {
        User user;
        MailAddress FromMailAddress;
        MailAddress ToMailAddress;
        string Subject;
        string TextBody;
        public DateTime time;
        /// <summary>
        /// Конструктор класса Message
        /// </summary>
        /// <param name="user">Пользователь, отправляющий сообщение</param>
        /// <param name="to">E-mail получателя</param>
        /// <param name="Subject">Тема сообщения</param>
        /// <param name="TextBody">Текст сообщения</param>
        public Message(User user, string to, string Subject, string TextBody)
        {
            this.user = user;
            FromMailAddress = new MailAddress(user.MailAddress);
            ToMailAddress = new MailAddress(to);
            this.Subject = Subject;
            this.TextBody = TextBody;
        }
        /// <summary>
        /// Метод, отправляющий сообщение через SMTP-сервер
        /// </summary>
        /// <param name="domen">Домен адреса отправителя, позволяющий определить
        /// SMTP-сервер, через который необходимо послать сообщение</param>
        public void SendMessage(string domen)
        {
            using (MailMessage mailMessage = new MailMessage(FromMailAddress, ToMailAddress))
            using (SmtpClient smtpClient = new SmtpClient())
            {
                mailMessage.Subject = Subject;
                mailMessage.Body = TextBody;
                
                // Рассматривает основные домены, используемые в России,
                // и назначает SMTP-сервер в соответствии с доменом
                switch (domen)
                {
                    case "gmail.com":
                        smtpClient.Host = "smtp.gmail.com";
                        break;
                    case "mail.ru":
                    case "bk.ru":
                    case "inbox.ru":
                    case "list.ru":
                        smtpClient.Host = "smtp.mail.ru";
                        break;
                    case "yandex.ru":
                        smtpClient.Host = "smtp.yandex.ru";
                        break;
                    case "rambler.ru":
                    case "lenta.ru":
                    case "autorambler.ru":
                    case "myrambler.ru":
                    case "ro.ru":
                    case "rambler.ua":
                        smtpClient.Host = "smtp.rambler.ru";
                        break;
                }
                // Устанавливаем номер порта для SMTP-сервера
                smtpClient.Port = 587;
                // Включаем протокол SSL, шифрующий данные
                smtpClient.EnableSsl = true;
                // Устанавливаем метод доставки сообщений
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                // Устанавливаем данные для авторизации
                smtpClient.Credentials = new NetworkCredential(FromMailAddress.Address, user.password);
                // Посылаем сообщение
                smtpClient.Send(mailMessage);
                // Засекаем время отправки
                time = DateTime.Now;
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            // Получаем текущий IP сервера и задаём порт, который будет прослушиваться
            string Host = Dns.GetHostName();
            string url = "http://" + Dns.GetHostByName(Host).AddressList[0].ToString();
            string port = "9999";
            string prefix = String.Format("{0}:{1}/", url, port);

            // Инициализация объекта библиотечного класса HttpListener, который прослушивает порт
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            Console.WriteLine("Listening on {0}...", prefix);
            int ID = 0;
            
            // Создание отдельного потока, исполняющего метод CheckDBToSend
            Thread t = new Thread(CheckDBToSend);
            t.Start();

            // Создание отдельного потока, исполняющего метод CheckDBToDelete
            Thread newt = new Thread(CheckDBToDelete);
            newt.Start();        

            // Бесконечный цикл, в котором постоянно ожидаются GET- и POST-запросы протокола HTTP
            while (true)
            {
                // Ожидание входящего запроса
                HttpListenerContext context = listener.GetContext();
                // Объект запроса
                HttpListenerRequest request = context.Request;
                // Объект ответа
                HttpListenerResponse response = context.Response;
                
                Console.WriteLine("{0} request was caught: {1}", request.HttpMethod, request.Url);
                response.StatusCode = (int)HttpStatusCode.OK;

                // Объявление переменных
                string FROMEmailAddress;       // адрес отправителя
                string EmailPassword;          // пароль отправителя
                User I;                        // пользователь-отправитель
                string TOEmailAddress;         // адрес получателя
                string Subject;                // тема сообщения
                string Text;                   // текст сообщения
                string Status;                 // статус сообщения
                string AddTime;                // время добавления сообщения в базу данных
                string domen;                  // домен

                // Принимаем POST-запрос
                if (request.HttpMethod == "POST")
                {
                    if (!request.HasEntityBody)
                    {
                        Console.WriteLine("No client data was sent with the request.");
                        return;
                    }
                    Stream body = request.InputStream;
                    Encoding encoding = request.ContentEncoding;
                    StreamReader reader = new System.IO.StreamReader(body, encoding);
                    string s = reader.ReadToEnd();

                    // Разбиение тела запроса на необходимые нам данные
                    s = s.Replace("%40", "@");
                    string[] words = s.Split('&');
                    words[0] = words[0].Remove(0, 6);
                    words[1] = words[1].Remove(0, 9);
                    words[2] = words[2].Remove(0, 3);
                    words[3] = words[3].Remove(0, 8);
                    words[4] = words[4].Remove(0, 5);

                    // Установление домена для последующей передачи его в метод SendMessage
                    int ind = words[0].IndexOf('@') + 1;
                    domen = words[0].Substring(ind);

                    DateTime nowtime = DateTime.Now;
                    body.Close();
                    reader.Close();

                    // Присваивание данных, полученных из запроса, ранее объявленным переменным
                    FROMEmailAddress = words[0];
                    EmailPassword = words[1];
                    I = new User(FROMEmailAddress, EmailPassword);
                    TOEmailAddress = words[2];
                    Subject = words[3];
                    Text = words[4];

                    // Определение статуса сообщения (не отправлено)
                    Status = "Message not sent";

                    // Определение времени добавления сообщения в базу данных
                    AddTime = nowtime.ToString();

                    // Установление соединения с базой данных
                    using (SQLiteConnection connection = new SQLiteConnection("Data Source=\\testik.db; Version=3"))
                    {
                        connection.Open();

                        // Выбор максимального id сообщения
                        SQLiteCommand selectid = connection.CreateCommand();
                        selectid.CommandText = ("SELECT id FROM message ORDER BY id DESC LIMIT 1");
                        using (SQLiteDataReader reader4 = selectid.ExecuteReader())
                        {
                            int newid = 0;
                            foreach (DbDataRecord record in reader4)
                            {
                                string idnew;
                                idnew = record["id"].ToString();
                                if (string.IsNullOrEmpty(idnew))
                                {
                                    ID = 0;
                                }
                                else
                                {
                                    newid = Convert.ToInt32(idnew);
                                    ID = newid;
                                }
                            }
                        }
                    connection.Close();

                    // Снова открываем соединение
                    connection.Open();
                    // Ищем, есть ли пользователь-отправитель в базе данных
                    SQLiteCommand selectuser = connection.CreateCommand();
                    selectuser.CommandText = ("SELECT email FROM users WHERE (email=@email)");
                    selectuser.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                    string result = "";
                    result = (string)selectuser.ExecuteScalar();
                    connection.Close();
                        // если такого пользователя нет
                        if (string.IsNullOrEmpty(result))
                        {
                            try
                            {
                                // отправляем тестовое сообщение на специальную почту,
                                // таким образом проверяя, верны ли email и пароль
                                Message testmsg = new Message(I, "testpochta10@bk.ru", "test", "test");
                                testmsg.SendMessage(domen);
                                Console.WriteLine("Authorization is passed!");
                                connection.Open();
                                // Заносим пользователя в таблицу users
                                SQLiteCommand insertcom = connection.CreateCommand();
                                insertcom.CommandText = ("INSERT INTO 'users' ('email', 'password') VALUES (@email, @password)");
                                insertcom.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                                insertcom.Parameters.Add("@password", System.Data.DbType.String).Value = EmailPassword;
                                insertcom.ExecuteNonQuery();
                                connection.Close();
                                connection.Open();
                                // Заносим его сообщение в таблицу message
                                ID++;
                                insertcom.CommandText = ("INSERT INTO 'message' ('id', 'fromemail', 'toemail', 'subject', 'textemail', 'status', 'addtime') VALUES (@id, @fromemail, @toemail, @subject, @textemail, @status, @addtime)");
                                insertcom.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                insertcom.Parameters.Add("@fromemail", System.Data.DbType.String).Value = FROMEmailAddress;
                                insertcom.Parameters.Add("@toemail", System.Data.DbType.String).Value = TOEmailAddress;
                                insertcom.Parameters.Add("@subject", System.Data.DbType.String).Value = Subject;
                                insertcom.Parameters.Add("@textemail", System.Data.DbType.String).Value = Text;
                                insertcom.Parameters.Add("@status", System.Data.DbType.String).Value = Status;
                                insertcom.Parameters.Add("@addtime", System.Data.DbType.String).Value = AddTime;
                                insertcom.ExecuteNonQuery();
                                connection.Close();
                                
                            }
                            catch
                            {
                                Console.WriteLine("Wrong e-mail or password, authorization is failed!");
                            }

                        }
                        // Если пользователь уже существует в таблице users
                        else
                        {
                            connection.Open();
                            // Достаем его пароль из таблицы users
                            SQLiteCommand selectpassword = connection.CreateCommand();
                            selectpassword.CommandText = ("SELECT password FROM users WHERE (email=@email)");
                            selectpassword.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                            SQLiteDataReader reader3 = selectpassword.ExecuteReader();
                            string newpassword = "";
                            foreach (DbDataRecord record in reader3)
                            {
                                newpassword = record["password"].ToString();
                                // Если введенный пароль не соответствует паролю из базы данных,
                                // выводим сообщение о том, что авторизация не пройдена
                                if (newpassword != EmailPassword)
                                {
                                    Console.WriteLine("Wrong e-mail or password, authorization is failed!");
                                }
                                // Если пароль верен, добавляем сообщение пользователя в таблицу message
                                else
                                {
                                    SQLiteCommand insertcom = connection.CreateCommand();
                                    ID++;
                                    insertcom.CommandText = ("INSERT INTO 'message' ('id', 'fromemail', 'toemail', 'subject', 'textemail', 'status', 'addtime') VALUES (@id, @fromemail, @toemail, @subject, @textemail, @status, @addtime)");
                                    insertcom.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                    insertcom.Parameters.Add("@fromemail", System.Data.DbType.String).Value = FROMEmailAddress;
                                    insertcom.Parameters.Add("@toemail", System.Data.DbType.String).Value = TOEmailAddress;
                                    insertcom.Parameters.Add("@subject", System.Data.DbType.String).Value = Subject;
                                    insertcom.Parameters.Add("@textemail", System.Data.DbType.String).Value = Text;
                                    insertcom.Parameters.Add("@status", System.Data.DbType.String).Value = Status;
                                    insertcom.Parameters.Add("@addtime", System.Data.DbType.String).Value = AddTime;
                                    insertcom.ExecuteNonQuery();
                                }
                            }
                            connection.Close();
                        }
                    }
                }
                
                using (Stream output = response.OutputStream)
                {
                    // Создание формы html
                    string responseStr = @"<!DOCTYPE HTML>
<html><head><meta charset = ""utf-8""></head><body>
 <form method=""post"" action=""/login"">
<p><b>Login: </b><br>
<input type=""text"" name=""login"" size=""40""></p>
<p><b>Password: </b><br>
<input type=""password"" name=""password"" size=""40""></p>
<p><b>To: </b><br>
<input type=""text"" name=""to"" size=""40""></p>
<p><b>Subject: </b><br>
<input type=""text"" name=""subject"" size=""40""></p>
<p><b>Text: </b><br>
<p><textarea name=""text""> </textarea></p>
<p><input type=""file"" name=""f"" multiple></p>
<p><input type=""submit"" value=""send""></p>
</form></body></html>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseStr);
                    // Получаем поток ответа и пишем в него ответ
                    response.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                    // Закрываем поток
                    output.Close();
                }
            }
        }

        /// <summary>
        /// Метод, содержащий бесконечный цикл, который проверяет наличие в таблице message
        /// сообщений со статусом "не отправлено" и с добавления которых прошло 10 минут и более
        /// </summary>
        static void CheckDBToSend()
        {
            while (true)
            {
                // Время отправления сообщения
                string SendTime = "";
                // Создаем и открываем соединение с БД
                using (SQLiteConnection newconnection = new SQLiteConnection("Data Source=\\testik.db; Version=3"))
                {
                    newconnection.Open();
                    DateTime timeadd;
                    SQLiteCommand sendmes = newconnection.CreateCommand();
                    // Ищем сообщения со статусом "не отправлено"
                    sendmes.CommandText = ("SELECT * FROM message WHERE (status='Message not sent')");
                    using (SQLiteDataReader reader5 = sendmes.ExecuteReader())
                    {
                        foreach (DbDataRecord record in reader5)
                        {
                            int id = Convert.ToInt32(record["id"]);
                            string fromemail = record["fromemail"].ToString();
                            string toemail = record["toemail"].ToString();
                            string subject = record["subject"].ToString();
                            string textemail = record["textemail"].ToString();
                            string status = record["status"].ToString();
                            string time = record["time"].ToString();
                            string addtime = record["addtime"].ToString();
                            timeadd = DateTime.Now;
                            DateTime newaddtime = Convert.ToDateTime(addtime);
                            SQLiteCommand selectpass = newconnection.CreateCommand();
                            selectpass.CommandText = ("SELECT password FROM users WHERE (email=@email)");
                            selectpass.Parameters.Add("@email", System.Data.DbType.String).Value = fromemail;
                            SQLiteDataReader reader6 = selectpass.ExecuteReader();
                            string newpass = "";
                            foreach (DbDataRecord newrecord in reader6)
                            {
                                newpass = newrecord["password"].ToString();
                            }
                            User J = new User(fromemail, newpass);
                            int ind = fromemail.IndexOf('@') + 1;
                            string domen = fromemail.Substring(ind);
                            // Если год, месяц, день и час равны, а разница между минутами >= 10, то отправляем сообщение
                            if ((timeadd.Year == newaddtime.Year) && (timeadd.Month == newaddtime.Month) && (timeadd.Day == newaddtime.Day) && (timeadd.Hour == newaddtime.Hour) && (timeadd.Minute - newaddtime.Minute >= 10))
                            {
                                Message myMessage = new Message(J, toemail, subject, textemail);
                                try
                                {
                                    myMessage.SendMessage(domen);
                                    Console.WriteLine("Message sent successfully!");
                                    SQLiteCommand changestatus = newconnection.CreateCommand();
                                    changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                    changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                    changestatus.ExecuteNonQuery();
                                    SendTime = myMessage.time.ToString();
                                    SQLiteCommand changetime = newconnection.CreateCommand();
                                    changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                    changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                    changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                    changetime.ExecuteNonQuery();
                                }
                                catch
                                {
                                    Console.WriteLine("Error!Message not sent!");
                                }
                            }
                            else
                            {
                                // Если год, месяц и день равны, час отличается на 1 (на стыке часов)
                                if ((timeadd.Year == newaddtime.Year) && (timeadd.Month == newaddtime.Month) && (timeadd.Day == newaddtime.Day) && (timeadd.Hour - newaddtime.Hour >= 1) && (timeadd.Minute - (newaddtime.Minute - 50) >= 0))
                                {
                                    Message myMessage = new Message(J, toemail, subject, textemail);
                                    try
                                    {
                                        myMessage.SendMessage(domen);
                                        Console.WriteLine("Message sent successfully!");
                                        SQLiteCommand changestatus = newconnection.CreateCommand();
                                        changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                        changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                        changestatus.ExecuteNonQuery();
                                        SendTime = myMessage.time.ToString();
                                        SQLiteCommand changetime = newconnection.CreateCommand();
                                        changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                        changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                        changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                        changetime.ExecuteNonQuery();
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Error!Message not sent!");
                                    }
                                }
                                else
                                {
                                    // Если год и месяц равны, день отличается на 1 (на стыке дней)
                                    if ((timeadd.Year == newaddtime.Year) && (timeadd.Month == newaddtime.Month) && (timeadd.Day - newaddtime.Day >= 1) && (timeadd.Hour - (newaddtime.Hour - 23) >= 0) && (timeadd.Minute - (newaddtime.Minute - 50) >= 0))
                                    {
                                        Message myMessage = new Message(J, toemail, subject, textemail);
                                        try
                                        {
                                            myMessage.SendMessage(domen);
                                            Console.WriteLine("Message sent successfully!");
                                            SQLiteCommand changestatus = newconnection.CreateCommand();
                                            changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                            changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                            changestatus.ExecuteNonQuery();
                                            SendTime = myMessage.time.ToString();
                                            SQLiteCommand changetime = newconnection.CreateCommand();
                                            changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                            changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                            changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                            changetime.ExecuteNonQuery();
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Error!Message not sent!");
                                        }
                                    }
                                    else
                                    {
                                        // Если год равен, месяц отличается на 1 (на стыке месяцев)
                                        if ((timeadd.Year == newaddtime.Year) && (timeadd.Month - newaddtime.Month >= 1) && (timeadd.Hour - (newaddtime.Hour - 23) >= 0) && (timeadd.Minute - (newaddtime.Minute - 50) >= 0))
                                        {
                                            Message myMessage = new Message(J, toemail, subject, textemail);
                                            try
                                            {
                                                myMessage.SendMessage(domen);
                                                Console.WriteLine("Message sent successfully!");
                                                SQLiteCommand changestatus = newconnection.CreateCommand();
                                                changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                                changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                                changestatus.ExecuteNonQuery();
                                                SendTime = myMessage.time.ToString();
                                                SQLiteCommand changetime = newconnection.CreateCommand();
                                                changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                                changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                                changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                                changetime.ExecuteNonQuery();
                                            }
                                            catch
                                            {
                                                Console.WriteLine("Error!Message not sent!");
                                            }
                                        }
                                        else
                                        {
                                            // Год отличается на 1 (на стыке годов)
                                            if ((timeadd.Year - newaddtime.Year >= 1) && (timeadd.Month - (newaddtime.Month - 12) >= 1) && (timeadd.Hour - (newaddtime.Hour - 23) >= 0) && (timeadd.Minute - (newaddtime.Minute - 50) >= 0))
                                            {
                                                Message myMessage = new Message(J, toemail, subject, textemail);
                                                try
                                                {
                                                    myMessage.SendMessage(domen);
                                                    Console.WriteLine("Message sent successfully!");
                                                    SQLiteCommand changestatus = newconnection.CreateCommand();
                                                    changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                                    changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                                    changestatus.ExecuteNonQuery();
                                                    SendTime = myMessage.time.ToString();
                                                    SQLiteCommand changetime = newconnection.CreateCommand();
                                                    changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                                    changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                                    changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                                    changetime.ExecuteNonQuery();
                                                }
                                                catch
                                                {
                                                    Console.WriteLine("Error!Message not sent!");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    newconnection.Close();
                }
                Thread.Sleep(500);
            }
        }
        /// <summary>
        /// Метод, содержащий бесконечный цикл, который проверяет наличие в таблице message
        /// сообщений со статусом "отправлено" и с отправления которых прошло 7 дней и более
        /// </summary>
        static void CheckDBToDelete()
        {
            while (true)
            {
                // Создаем и открываем соединение с БД
                using (SQLiteConnection secondconnection = new SQLiteConnection("Data Source=\\testik.db; Version=3"))
                {
                    secondconnection.Open();
                    SQLiteCommand deletemes = secondconnection.CreateCommand();
                    // Ищем сообщения со статусом "отправлено"
                    deletemes.CommandText = ("SELECT * FROM message WHERE (status='Message sent')");
                    using (SQLiteDataReader reader7 = deletemes.ExecuteReader())
                    {
                        foreach (DbDataRecord record in reader7)
                        {
                            int id = Convert.ToInt32(record["id"]);
                            string fromemail = record["fromemail"].ToString();
                            string toemail = record["toemail"].ToString();
                            string subject = record["subject"].ToString();
                            string textemail = record["textemail"].ToString();
                            string status = record["status"].ToString();
                            string time = record["time"].ToString();
                            DateTime nowtime = DateTime.Now;
                            DateTime newtime = Convert.ToDateTime(time);

                            // Если год и месяц равны, разница в днях больше или равна 7
                            if ((nowtime.Year == newtime.Year) && (nowtime.Month == newtime.Month) && (nowtime.Day - newtime.Day >= 7))
                            {
                                SQLiteCommand deletemessage = secondconnection.CreateCommand();
                                deletemessage.CommandText = ("DELETE FROM message WHERE (id=@id)");
                                deletemessage.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                deletemessage.ExecuteNonQuery();
                                Console.WriteLine("Message was deleted");
                            }
                            else
                            {
                                // Если год равен, месяц отличается на 1 (на стыке месяцев)
                                if ((nowtime.Year == newtime.Year) && (nowtime.Month - newtime.Month >= 1) && (newtime.Day - 30 - nowtime.Day >= 7))
                                {
                                    SQLiteCommand deletemessage = secondconnection.CreateCommand();
                                    deletemessage.CommandText = ("DELETE FROM message WHERE (id=@id)");
                                    deletemessage.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                    deletemessage.ExecuteNonQuery();
                                    Console.WriteLine("Message was deleted");
                                }
                                else
                                {
                                    // Если год отличается на 1 (на стыке годов)
                                    if ((nowtime.Year - newtime.Year >= 1) && (newtime.Day - 30 - nowtime.Day >= 7))
                                    {
                                        SQLiteCommand deletemessage = secondconnection.CreateCommand();
                                        deletemessage.CommandText = ("DELETE FROM message WHERE (id=@id)");
                                        deletemessage.Parameters.Add("@id", System.Data.DbType.Int32).Value = id;
                                        deletemessage.ExecuteNonQuery();
                                        Console.WriteLine("Message was deleted");
                                    }
                                }
                            }
                        }
                    }
                    secondconnection.Close();
                }
            }
        }
    }
}
