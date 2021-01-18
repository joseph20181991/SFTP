using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPfileTransferService.Utility
{
    public static class LoggerSFTPService
    {

        //private static string path = @"C:\Log\SmsBillNRawStransactionLog.txt";

        private static readonly object LockObj = new object();

        public static void FileCreation(string strDescription, string path)
        {

            lock (LockObj)
            {
                if (!File.Exists(path))
                {
                    using (FileStream fs = File.Create(path))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("creating the file" + Environment.NewLine);

                        fs.Write(info, 0, info.Length);
                    }
                    using (StreamReader sr = File.OpenText(path))
                    {
                        string s = "";
                        while ((s = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(s);
                        }
                    }
                }

                if (File.Exists(path))
                {
                    using (var tw = new StreamWriter(path, true))
                    {
                        tw.WriteLine(DateTime.Now + ": " + strDescription);
                        tw.Close();
                    }
                }
            }



        }
    }
}
