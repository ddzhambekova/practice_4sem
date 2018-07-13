using System;
using System.Data.SQLite;
using System.Data.Common;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace sqtest
{
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
    class Message
    {
        User user;
        MailAddress FromMailAddress;
        MailAddress ToMailAddress;
        string Subject;
        string TextBody;
        string file = "0";
        public DateTime time;
        public Message(User user, string to, string Subject, string TextBody)
        {
            this.user = user;
            FromMailAddress = new MailAddress(user.MailAddress);
            ToMailAddress = new MailAddress(to);
            this.Subject = Subject;
            this.TextBody = TextBody;
        }
        public Message(User user, string to, string Subject, string TextBody, string file)
        {
            this.user = user;
            FromMailAddress = new MailAddress(user.MailAddress);
            ToMailAddress = new MailAddress(to);
            this.Subject = Subject;
            this.TextBody = TextBody;
            this.file = file;
        }
        public void SendMessage(string domen)
        {
            using (MailMessage mailMessage = new MailMessage(FromMailAddress, ToMailAddress))
            using (SmtpClient smtpClient = new SmtpClient())
            {
                mailMessage.Subject = Subject;
                mailMessage.Body = TextBody;

                if (file != "0")
                {
                    Attachment attach = new Attachment(file);
                    mailMessage.Attachments.Add(attach);
                }

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

                smtpClient.Port = 587;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(FromMailAddress.Address, user.password);
                smtpClient.Send(mailMessage);
                time = DateTime.Now;
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://192.168.0.105";
            string port = "9999";
            string prefix = String.Format("{0}:{1}/", url, port);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            listener.Start();

            Console.WriteLine("Welcome to simple HttpListener.\n", port);
            Console.WriteLine("Listening on {0}...", prefix);
            int ID = 0;

            while (true)
            {
                //Ожидание входящего запроса
                HttpListenerContext context = listener.GetContext();
                //Объект запроса
                HttpListenerRequest request = context.Request;
                //Объект ответа
                HttpListenerResponse response = context.Response;


                Console.WriteLine("{0} request was caught: {1}",
                                   request.HttpMethod, request.Url);

                response.StatusCode = (int)HttpStatusCode.OK;

                //принимаем post-сообщение
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
                    //if (request.ContentType != null)
                    //{
                    //    Console.WriteLine("Client data content type {0}", request.ContentType);
                    //}
                    //Console.WriteLine("Client data content length {0}", request.ContentLength64);

                    //Console.WriteLine("Start of client data:");
                    // Convert the data to a string and display it on the console.
                    string s = reader.ReadToEnd();
                    //Console.WriteLine(s);
                    s = s.Replace("%40", "@");
                    string[] words = s.Split('&');
                    words[0] = words[0].Remove(0, 6);
                    words[1] = words[1].Remove(0, 9);
                    words[2] = words[2].Remove(0, 3);
                    words[3] = words[3].Remove(0, 8);
                    words[4] = words[4].Remove(0, 5);
                    int ind = words[0].IndexOf('@') + 1;
                    string domen = words[0].Substring(ind);
                    //Console.WriteLine("domen " + domen);

                    //Console.WriteLine(words[0]);
                    //for (int i = 2; i < 5; i++)
                    //{
                    //    Console.WriteLine(words[i]);
                    //}
                    //Console.WriteLine("End of client data:");
                    body.Close();
                    reader.Close();
                    string FROMEmailAddress = words[0];
                    string EmailPassword = words[1];
                    User I = new User(FROMEmailAddress, EmailPassword);
                    string TOEmailAddress = words[2];
                    string Subject = words[3];
                    string Text = words[4];
                    string Attachment1 = "hdkshkd";
                    string Attachment2 = "hdkshkd";
                    string Attachment3 = "hdkshkd";
                    string Attachment4 = "hdkshkd";
                    string Attachment5 = "hdkshkd";
                    string Status = "Message not sent";
                    string SendTime = "";
                    SQLiteConnection connection = new SQLiteConnection("Data Source=C:\\testik.db; Version=3");
                    connection.Open();
                    SQLiteCommand selectuser = connection.CreateCommand();
                    selectuser.CommandText = ("SELECT email FROM users WHERE (email=@email)");
                    selectuser.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                    string result = "";
                    result = (string)selectuser.ExecuteScalar();
                    connection.Close();
                    if (string.IsNullOrEmpty(result))
                    {
                        try
                        {
                            Message testmsg = new Message(I, "testpochta10@bk.ru", "test", "test");
                            testmsg.SendMessage(domen);
                            Console.WriteLine("Authorization is passed!");
                            connection.Open();
                            SQLiteCommand insertcom = connection.CreateCommand();
                            insertcom.CommandText = ("INSERT INTO 'users' ('email', 'password') VALUES (@email, @password)");
                            insertcom.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                            insertcom.Parameters.Add("@password", System.Data.DbType.String).Value = EmailPassword;
                            insertcom.ExecuteNonQuery();
                            connection.Close();
                            connection.Open();
                            insertcom.CommandText = ("INSERT INTO 'message' ('fromemail', 'toemail', 'subject', 'textemail', 'attachment1', 'attachment2', 'attachment3', 'attachment4', 'attachment5', 'status') VALUES (@fromemail, @toemail, @subject, @textemail, @attachment1, @attachment2, @attachment3, @attachment4, @attachment5, @status)");
                            insertcom.Parameters.Add("@fromemail", System.Data.DbType.String).Value = FROMEmailAddress;
                            insertcom.Parameters.Add("@toemail", System.Data.DbType.String).Value = TOEmailAddress;
                            insertcom.Parameters.Add("@subject", System.Data.DbType.String).Value = Subject;
                            insertcom.Parameters.Add("@textemail", System.Data.DbType.String).Value = Text;
                            insertcom.Parameters.Add("@attachment1", System.Data.DbType.String).Value = Attachment1;
                            insertcom.Parameters.Add("@attachment2", System.Data.DbType.String).Value = Attachment2;
                            insertcom.Parameters.Add("@attachment3", System.Data.DbType.String).Value = Attachment3;
                            insertcom.Parameters.Add("@attachment4", System.Data.DbType.String).Value = Attachment4;
                            insertcom.Parameters.Add("@attachment5", System.Data.DbType.String).Value = Attachment5;
                            insertcom.Parameters.Add("@status", System.Data.DbType.String).Value = Status;
                            insertcom.ExecuteNonQuery();
                            connection.Close();

                            Message myMessage = new Message(I, TOEmailAddress, Subject, Text);
                            try
                            {
                                myMessage.SendMessage(domen);
                                Console.WriteLine("Message sent successfully!");
                                ID++;
                                connection.Open();
                                SQLiteCommand changestatus = connection.CreateCommand();
                                changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                changestatus.ExecuteNonQuery();
                                SendTime = myMessage.time.ToString();
                                SQLiteCommand changetime = connection.CreateCommand();
                                changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                changetime.ExecuteNonQuery();
                                connection.Close();
                            }
                            catch
                            {
                                Console.WriteLine("Error!Message not sent!");
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Wrong e-mail or password, authorization is failed!");
                        }

                    }
                    else
                    {
                        connection.Open();
                        SQLiteCommand selectpassword = connection.CreateCommand();
                        selectpassword.CommandText = ("SELECT password FROM users WHERE (email=@email)");
                        selectpassword.Parameters.Add("@email", System.Data.DbType.String).Value = FROMEmailAddress;
                        SQLiteDataReader reader3 = selectpassword.ExecuteReader();
                        string newpassword = "";
                        foreach (DbDataRecord record in reader3)
                        {
                            newpassword = record["password"].ToString();
                            if (newpassword != EmailPassword)
                            {
                                Console.WriteLine("Wrong e-mail or password, authorization is failed!");
                            }
                            else
                            {
                                //connection.Open();
                                SQLiteCommand insertcom = connection.CreateCommand();
                                insertcom.CommandText = ("INSERT INTO 'message' ('fromemail', 'toemail', 'subject', 'textemail', 'attachment1', 'attachment2', 'attachment3', 'attachment4', 'attachment5', 'status') VALUES (@fromemail, @toemail, @subject, @textemail, @attachment1, @attachment2, @attachment3, @attachment4, @attachment5, @status)");
                                insertcom.Parameters.Add("@fromemail", System.Data.DbType.String).Value = FROMEmailAddress;
                                insertcom.Parameters.Add("@toemail", System.Data.DbType.String).Value = TOEmailAddress;
                                insertcom.Parameters.Add("@subject", System.Data.DbType.String).Value = Subject;
                                insertcom.Parameters.Add("@textemail", System.Data.DbType.String).Value = Text;
                                insertcom.Parameters.Add("@attachment1", System.Data.DbType.String).Value = Attachment1;
                                insertcom.Parameters.Add("@attachment2", System.Data.DbType.String).Value = Attachment2;
                                insertcom.Parameters.Add("@attachment3", System.Data.DbType.String).Value = Attachment3;
                                insertcom.Parameters.Add("@attachment4", System.Data.DbType.String).Value = Attachment4;
                                insertcom.Parameters.Add("@attachment5", System.Data.DbType.String).Value = Attachment5;
                                insertcom.Parameters.Add("@status", System.Data.DbType.String).Value = Status;
                                insertcom.ExecuteNonQuery();
                                //connection.Close();

                                Message myMessage = new Message(I, TOEmailAddress, Subject, Text);
                                try
                                {
                                    myMessage.SendMessage(domen);
                                    Console.WriteLine("Message sent successfully!");
                                    ID++;
                                    //connection.Open();
                                    SQLiteCommand changestatus = connection.CreateCommand();
                                    changestatus.CommandText = ("UPDATE message SET status='Message sent' WHERE id=@id;");
                                    changestatus.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                    changestatus.ExecuteNonQuery();
                                    SendTime = myMessage.time.ToString();
                                    SQLiteCommand changetime = connection.CreateCommand();
                                    changetime.CommandText = ("UPDATE message SET time=@time WHERE id=@id;");
                                    changetime.Parameters.Add("@id", System.Data.DbType.Int32).Value = ID;
                                    changetime.Parameters.Add("@time", System.Data.DbType.String).Value = SendTime;
                                    changetime.ExecuteNonQuery();
                                    //connection.Close();
                                }
                                catch
                                {
                                    Console.WriteLine("Error!Message not sent!");
                                }
                            }
                        }
                        connection.Close();
                    }
                }

                //Возвращаем ответ
                using (Stream output = response.OutputStream)
                { // создаем ответ в виде кода html
                    string responseStr = @"<!DOCTYPE HTML>
<html><head></head><body>
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
<p><input type=""submit"" value=""send""></p>
</form></body></html>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseStr);
                    // получаем поток ответа и пишем в него ответ
                    response.ContentLength64 = buffer.Length;
                    output.Write(buffer, 0, buffer.Length);
                    // закрываем поток
                    output.Close();
                }
            }


            //вывод очереди сообщений
            //connection.Open();
            //SQLiteCommand showlist = connection.CreateCommand();
            //showlist.CommandText = ("SELECT * FROM message WHERE fromemail=@fromemail;");
            //showlist.Parameters.Add("@fromemail", System.Data.DbType.String).Value = FROMEmailAddress;
            //SQLiteDataReader reader2 = showlist.ExecuteReader();
            //foreach (DbDataRecord record in reader2)
            //{
            //    string fromemail = record["fromemail"].ToString();
            //    string toemail = record["toemail"].ToString();
            //    string subject = record["subject"].ToString();
            //    string textemail = record["textemail"].ToString();
            //    string attachment1 = record["attachment1"].ToString();
            //    string attachment2 = record["attachment2"].ToString();
            //    string attachment3 = record["attachment3"].ToString();
            //    string attachment4 = record["attachment4"].ToString();
            //    string attachment5 = record["attachment5"].ToString();
            //    string status = record["status"].ToString();
            //    string time = record["time"].ToString();
            //}
            //connection.Close();
        }
    }
}