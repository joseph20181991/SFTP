using Renci.SshNet;
using SFTPfileTransferService.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SFTPfileTransferService
{
    public partial class FTPFileTransService : ServiceBase
    {
        string logPath;
        public FTPFileTransService()
        {
            InitializeComponent();
            logPath = ConfigurationManager.AppSettings[@"LogPath"];
        }

        public void onDebug()

        {
            OnStart(null);
        }


        protected override void OnStart(string[] args)
        {
            LoggerSFTPService.FileCreation("Service started", logPath);
            this.ScheduleService();
        }




        protected override void OnStop()
        {
            LoggerSFTPService.FileCreation("Service Stop", logPath);

            this.Schedular.Dispose();
        }

        private Timer Schedular;

        public void ScheduleService()
        {
            RunSFTPFileTransfer();
            try
            {
                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                LoggerSFTPService.FileCreation("Service Mode: " + mode, logPath);

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(System.Configuration.ConfigurationManager.AppSettings["ScheduledTime"]);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                LoggerSFTPService.FileCreation("Service scheduled to run after: " + schedule, logPath);

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LoggerSFTPService.FileCreation("Service Error on:" + ex.Message + ex.StackTrace, logPath);
                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("SimpleService"))
                {
                    LoggerSFTPService.FileCreation("Service stop for exception:", logPath);
                    serviceController.Stop();
                }
            }
        }


        private void SchedularCallback(object e)
        {
            LoggerSFTPService.FileCreation("Simple Service SchedularCallback() Log", logPath);
            LoggerSFTPService.FileCreation("(1)caling RunSFTPFileTransfer()", logPath);
            RunSFTPFileTransfer();

            this.ScheduleService();
        }

        private bool RunSFTPFileTransfer()
        {
            try
            {
                LoggerSFTPService.FileCreation("(2)in RunSFTPFileTransfer()", logPath);

                string Host = ConfigurationManager.AppSettings["SFTPHost"];
                int    Port = Convert.ToInt32(ConfigurationManager.AppSettings["SFTPPort"]);
                string Username = ConfigurationManager.AppSettings["SFTPUsername"];
                string Password = ConfigurationManager.AppSettings["SFTPPassword"];
                string remoteDirectory = ConfigurationManager.AppSettings["remoteDirectory"];
                string localDirectory = ConfigurationManager.AppSettings["LocalDirectory"];
                string fileNameStartWith = ConfigurationManager.AppSettings["remoteDrectryFileNameStrat"];
                int fileCreateDay = Convert.ToInt32(ConfigurationManager.AppSettings["fileCreateDay"]);
                var EmailAddressListString = ConfigurationManager.AppSettings["RecipientsEmailAddressList"];
                string[] EmailArray = EmailAddressListString.Split(',');


                var EmailSubject = ConfigurationManager.AppSettings["EmailSubject"];
                var EmailBody = ConfigurationManager.AppSettings["EmailBody"];

                LoggerSFTPService.FileCreation("(2)in making EmailList", logPath);


                using (var sftp = new SftpClient(Host, Port, Username, Password))
                {
                    LoggerSFTPService.FileCreation("(3)in sftp before client create", logPath);

                    sftp.Connect();
                    LoggerSFTPService.FileCreation("(4)in sftp client create success", logPath);

                    var files = sftp.ListDirectory(remoteDirectory);

                    LoggerSFTPService.FileCreation("(5) files list from remote by SSH.Number of file:" + files.Count() + "", logPath);

                    foreach (var file in files)
                    {
                        string remoteFileName = file.Name;
                        var dd = file.LastWriteTime.Date;
                        //   if ((!file.Name.StartsWith(".")) && ((file.LastWriteTime.Date == DateTime.Today))
                        if (file.Name.StartsWith(fileNameStartWith) && file.LastWriteTime.Date == DateTime.Today.AddDays(fileCreateDay))//MobileRechargeErrorCodeDetails                           
                            using (Stream file1 = File.OpenWrite(localDirectory + remoteFileName))
                            {
                                LoggerSFTPService.FileCreation("(6) file name:" + remoteFileName + " to send email", logPath);
                                sftp.DownloadFile(remoteDirectory + remoteFileName, file1);
                                LoggerSFTPService.FileCreation("(7) file download in desire folder: " + localDirectory + remoteFileName + "", logPath);

                                if (EmailArray.Count() > 0)
                                    foreach (var email in EmailArray)
                                    {
                                        LoggerSFTPService.FileCreation("(8) in loop of sending email ", logPath);
                                        var strimFile = sftp.OpenRead(remoteDirectory + remoteFileName);
                                        LoggerSFTPService.FileCreation("(9) in loop of strim strem file ", logPath);
                                        SendEmailT(EmailSubject, EmailBody, email, strimFile);
                                    }
                            }
                    }
                    sftp.Disconnect();
                    LoggerSFTPService.FileCreation("sftp Disconnect successfully", logPath);

                }

            }
            catch (Exception ex)
            {
                LoggerSFTPService.FileCreation("sftp Exception:" + ex.Message.ToString() + "", logPath);

                Console.WriteLine(ex.Message.ToString());
            }


            return true;
        }

        public static void SendEmailT(string subject, string body, string recipients, Stream file)
        {
            var logPath = ConfigurationManager.AppSettings[@"LogPath"];

            LoggerSFTPService.FileCreation("(10) in loop; Send Email", logPath);

            SmtpClient client = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["SmtpHost"],
                Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]),
                EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["SmtpSsl"]),
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(
                    userName: ConfigurationManager.AppSettings["EmailUserName"],
                    password: ConfigurationManager.AppSettings["EmailPassword"]
                )
            };
            LoggerSFTPService.FileCreation("(11) in loop; SmtpClient create success", logPath);

            var mailMessage = new MailMessage(
                from: ConfigurationManager.AppSettings["EmailFrom"],
                to: recipients
            )
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            var containType = ConfigurationManager.AppSettings["contenType"];
            var fileName = ConfigurationManager.AppSettings["fileName"];

            ContentType xlsxContent = new ContentType(containType);
            //"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            //"text/csv"
            Attachment data = new Attachment(file, xlsxContent);
            data.Name = fileName;
            mailMessage.Attachments.Add(data);
            LoggerSFTPService.FileCreation("(12) in loop; file Attachment success", logPath);

            try
            {
                client.Send(mailMessage);
                LoggerSFTPService.FileCreation("(13) in loop; Send mail success", logPath);

            }
            catch (Exception ex)
            {
                LoggerSFTPService.FileCreation("email exception:" + ex.Message.ToString() + "", logPath);

                Console.WriteLine(ex.Message.ToString());
            }

        }
    }
}
