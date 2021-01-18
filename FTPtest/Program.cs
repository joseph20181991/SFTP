using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace FTPtest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {


                var ddd = DateTime.Today.AddDays(-1);

                string url = ConfigurationManager.AppSettings["URL"];
                string userName = ConfigurationManager.AppSettings["UserName"];
                string password = ConfigurationManager.AppSettings["Password"];

                var EmailAddressListString = ConfigurationManager.AppSettings["RecipientsEmailAddressList"];
                string[] EmailArray = EmailAddressListString.Split(',');


                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));//

                string Host = "192.168.88.81";
                int Port = 22;
                string RemoteFileName = "test.txt";
                string LocalDestinationFilename = "TheDataFile.txt";
                string Username = "****";
                string Password = "******";
                string remoteDirectory = "/Rcis_FTP/";
              
                string localDirectory = @"F:\FilePuller\";

                using (var sftp = new SftpClient(Host, Port, Username, Password))
                {
                     sftp.Connect();
                     var files = sftp.ListDirectory(remoteDirectory);

                    foreach (var file in files)
                    {
                        string remoteFileName = file.Name;
                        var dd = file.LastWriteTime.Date;
                        var dddd = file.LastWriteTime.Date;
                        //   if ((!file.Name.StartsWith(".")) && ((file.LastWriteTime.Date == DateTime.Today))
                        if (file.Name.StartsWith("IBFund_BBLIB_CashBab"))// && (file.LastWriteTime.Date == DateTime.Today))   //MobileRechargeErrorCodeDetails
                            using (Stream file1 = File.OpenWrite(localDirectory + remoteFileName))
                        {
                                 //var ddd= sftp.OpenRead(remoteDirectory + remoteFileName);
                                 sftp.DownloadFile(remoteDirectory + remoteFileName, file1);

                               // SendEmailT("", "", "", ddd);

                        }
                    }
                    sftp.Disconnect();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }


        }



        public static void SendEmailT(string subject, string body, string recipients,Stream file)
        {





            //        "Host": "smtp.elasticemail.com",
            //"Port": "587",
            //"Ssl": "true",
            //"Username": "arif@r-cis.com",
            //"Password": "d964785d-19a9-4198-9829-373e4f51f65e",
            //"FromMail": "no-reply@cashbaba.com.bd"



            SmtpClient client = new SmtpClient
            {
                Host = "smtp.elasticemail.com",
                Port = 587,
                EnableSsl = true, //_configuration.GetValue<bool>("Smtp:Ssl"),
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(
                    userName: "****@r-cis.com",
                    password: "******-19a9-4198-9829-*******"
                )
            };

            subject = "SFTP Test";
            body = "Test Body";

            var mailMessage = new MailMessage(
                from: "******@*****.com.bd",
                to: "****h@****.com"
            )
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            ContentType xlsxContent = new ContentType("text/csv");
            //"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            //"text/csv"
            Attachment data = new Attachment(file, xlsxContent);
            data.Name = "SomeFilei.csv";
            mailMessage.Attachments.Add(data);

            try
            {
                client.Send(mailMessage);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            


        }



    }
}
