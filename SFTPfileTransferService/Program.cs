using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SFTPfileTransferService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

#if DEBUG
            FTPFileTransService service = new FTPFileTransService();
            service.onDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
             ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FTPFileTransService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
