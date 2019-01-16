using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GetInstalledPrograms.Service
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new GipService()
            //};
            //ServiceBase.Run(ServicesToRun);


            if (Environment.UserInteractive)
            {
                GipService gipService = new GipService();

                Console.WriteLine("服務啟動中...");
                gipService.Start();

                Console.WriteLine("服務已啟動，請按下 Enter 鍵關閉服務...");
                Console.WriteLine("---------------------------------------");
                Console.ReadKey();

                gipService.Stop();

                Console.WriteLine("服務已關閉");
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new GipService() };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
