using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using App_Class;
using Model;
using Newtonsoft.Json;

namespace GetInstalledPrograms.Service
{
    public partial class GipService : ServiceBase
    {
        private const string serverUrl = "https://192.168.60.52:18002/CustomEvent/Incoming/";
        static string programsPath = Path.Combine(Directory.GetCurrentDirectory(), "InstalledList");
        static string programsTestPath = Path.Combine(Directory.GetCurrentDirectory(), "InstalledListTest.txt");
        static string publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "PublicKey");
        static string privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "PrivateKey");
        static string logPath = Path.Combine(Directory.GetCurrentDirectory(), "EventLog.txt");
        static List<InstalledProgram> installedPrograms = new List<InstalledProgram>();
        static RSACryption rsaCryption;

        public GipService()
        {
            InitializeComponent();
        }

        public void Start()
        {
            string txt = string.Format("事件時間:{0} 事件名稱:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "服務啟動");
            AppLib.WriteToFile(txt, logPath, false);

            //有檔案 並與目前取得清單比對(避免服務停止時有
            if (File.Exists(programsPath) && File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
            {
                rsaCryption = new RSACryption(AppLib.ReadFromFile(publicKeyPath), AppLib.ReadFromFile(privateKeyPath));
                ReadProgramsList(programsPath);
                List <InstalledProgram> nowInstalledPrograms = InstalledProgram.GetInstalledPrograms();
                CompareAndWriteLog(nowInstalledPrograms, installedPrograms);
            }
            //第一次 or 遺失任一檔案
            else
            {
                rsaCryption = new RSACryption();
                AppLib.WriteToFile(rsaCryption.PublicKey, publicKeyPath);
                AppLib.WriteToFile(rsaCryption.PrivateKey, privateKeyPath);
                installedPrograms = InstalledProgram.GetInstalledPrograms();
                WriteProgramsList(installedPrograms, programsPath);
            }

            //寫出目前取得的軟體名稱
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("目前記錄取得{0}筆已安裝軟體", installedPrograms.Count);
            foreach (var item in installedPrograms)
            {
                Console.WriteLine(item.DisplayName);
            }
            Console.WriteLine("---------------------------------------");

            //啟動監聽
            WMIReceiveEvent();

        }

        protected override void OnStart(string[] args)
        {
            string txt = string.Format("事件時間:{0} 事件名稱:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "服務啟動"); 
            AppLib.WriteToFile(txt, logPath, false);
            Console.WriteLine(txt);
            Start();
        }

        protected override void OnStop()
        {
            string txt = string.Format("事件時間:{0} 事件名稱:{1} \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "服務停止");
            AppLib.WriteToFile(txt, logPath, false);
            Console.WriteLine(txt);
        }

        private void WriteProgramsList(List<InstalledProgram> installedData, string path)
        {

            try
            {
                List<InstalledListData> data = new List<InstalledListData>();
                foreach (var item in installedData)
                {
                    InstalledListData test = new InstalledListData(item.DisplayName, item.Version, item.EstimatedSize, item.InstallDate);
                    data.Add(test);
                }

                //轉成JSON
                string strJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                //RSA加密
                string encryptJson = rsaCryption.EncryptRsa(strJson);
                //寫入File
                AppLib.WriteToFile(encryptJson, path);
                AppLib.WriteToFile(strJson, programsTestPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WriteProgramsList" + ex.Message);
            }
        }

        private void ReadProgramsList(string path)
        {
            List<InstalledListData> data = new List<InstalledListData>();
            try
            {
                //讀取file
                string encrypedtJson = AppLib.ReadFromFile(path);
                //解密
                string decryptJson = rsaCryption.DecryptRsa(encrypedtJson);
                //轉成 InstalledProgram
                data = JsonConvert.DeserializeObject<List<InstalledListData>>(decryptJson);
                foreach (var item in data)
                {
                    installedPrograms.Add(new InstalledProgram(item.DisplayName, item.Version, item.InstallDate, item.EstimatedSize));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadProgramsList" + ex.Message);
            }
        }

        private void WMIReceiveEvent()
        {
            try
            {
                ManagementEventWatcher watcher = new ManagementEventWatcher();
                //Construct the query. Keypath specifies the key in the registry to watch.
                //Note the KeyPath should be must have backslashes escaped. Otherwise you 
                //will get ManagementException.
                string querystring = string.Format("SELECT * FROM RegistryKeyChangeEvent WHERE (KeyPath ='{0}' or KeyPath ='{1}') and Hive='{2}' "
                    , @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                    , @"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                    , "HKEY_LOCAL_MACHINE"
                    );
                WqlEventQuery query = new WqlEventQuery(querystring);
                Console.WriteLine("Waiting for an event...");
                Console.WriteLine("---------------------------------------");

                watcher.EventArrived += new EventArrivedEventHandler(HandleEvent);
                watcher.Query = query;

                // Start listening for events
                watcher.Start();

            }
            catch (ManagementException err)
            {
                Console.WriteLine("An error occurred while trying to receive an event: " + err.Message);
            }
        }

        private void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            List<InstalledProgram> installedProgramsNew = InstalledProgram.GetInstalledPrograms();
            CompareAndWriteLog(installedProgramsNew, installedPrograms);

            //var properties = e.NewEvent.Properties;

            //foreach (var p in properties)
            //{
            //    Console.WriteLine("{0} -- {1}", p.Name, p.Value);
            //}
            //Console.WriteLine("---------------------------------------");
        }

        private void CompareAndWriteLog(List<InstalledProgram> newList, List<InstalledProgram> originalList)
        {
            List<InstalledListDataLog> dataLog = new List<InstalledListDataLog>();

            try
            {
                //新增
                if (newList.Count > originalList.Count)
                {
                    foreach (var item in newList)
                    {
                        if (!originalList.Contains(item))
                        {
                            dataLog.Add(new InstalledListDataLog(item, Enum.GetName(typeof(EventType), EventType.新增程式)));
                        }
                    }
                }
                //移除
                else
                {
                    foreach (var item in originalList)
                    {
                        if (!newList.Contains(item))
                        {
                            dataLog.Add(new InstalledListDataLog(item, Enum.GetName(typeof(EventType), EventType.移除程式)));
                        }
                    }
                }

                if (dataLog.Count > 0)
                {
                    string logString = string.Empty;
                    foreach (var item in dataLog)
                    {
                        logString = string.Format("事件時間:{0} 事件名稱:{1} 程式名稱:{2} 安裝時間:{3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                    , item.EventType, item.Name,item.InstallDate);
                    }
                    AppLib.WriteToFile(logString, logPath, false);
                    WriteProgramsList(newList, programsPath);
                    installedPrograms = newList;
                    Console.WriteLine(logString);
                    Console.WriteLine("目前記錄取得{0}筆已安裝軟體(更新)", installedPrograms.Count);
                    Console.WriteLine("---------------------------------------");
                }
            }
            catch (ManagementException err)
            {
                Console.WriteLine("CompareAndWriteLog: " + err.Message);
            }


        }

        //private void UploadAPI(string jsonDataString)
        //{
        //    WebClient client = new WebClient();

        //    try
        //    {
        //        //Uri uu = new Uri("https://192.168.1.250:18002/CustomEvent/Incoming");
        //        Uri url = new Uri(serverUrl);
        //        client.Encoding = Encoding.UTF8;
        //        client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
        //        client.up
        //        ServicePointManager.ServerCertificateValidationCallback += (sender1, cert, chain, sslPolicyErrors) => true;
        //        client.UploadDataCompleted += Client_UploadDataCompleted;
        //        client.UploadDataAsync(url, "POST", Encoding.UTF8.GetBytes(jsonDataString));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Write(ex.Message);
        //    }

        //    using (WebClient wc = new WebClient())
        //    {
        //        try
        //        {
        //            wc.Encoding = Encoding.UTF8;

        //            NameValueCollection nc = new NameValueCollection();
        //            nc["id"] = "aaa";
        //            nc["pw"] = "bbb";

        //            byte[] bResult = wc.UploadValues(Config.PostURL, nc);

        //            string resultXML = Encoding.UTF8.GetString(bResult);
        //        }
        //        catch (WebException ex)
        //        {
        //            throw new Exception("無法連接遠端伺服器");
        //        }
        //    }
        //}
    }
}
