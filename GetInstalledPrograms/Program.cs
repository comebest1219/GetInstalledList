using GetInstalledPrograms.App_Class;
using GetInstalledPrograms.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;

namespace GetInstalledPrograms
{

    class Program
    {
        static string programsPath = Path.Combine(Directory.GetCurrentDirectory(), "InstalledList");
        static string programsTestPath = Path.Combine(Directory.GetCurrentDirectory(), "InstalledListTest.txt");
        static string publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "PublicKey");
        static string privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "PrivateKey");
        static string logPath = Path.Combine(Directory.GetCurrentDirectory(), "EventLog");
        static List<InstalledProgram> installedPrograms = new List<InstalledProgram>();
        static RSACryption rsaCryption;

        public enum EventType
        {
            新增程式=1,
            移除程式=2
        }

        static void Main(string[] args)
        {
            
            if (File.Exists(programsPath) && File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
            {
                rsaCryption = new RSACryption(AppLib.ReadFromFile(publicKeyPath), AppLib.ReadFromFile(privateKeyPath));
                ReadProgramsList(programsPath);
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

            Console.WriteLine("目前記錄取得{0}筆已安裝軟體", installedPrograms.Count);
            foreach (var item in installedPrograms)
            {
                Console.WriteLine(item.DisplayName);
            }
            Console.WriteLine("---------------------------------------");
            WMIReceiveEvent();
            Console.ReadKey();
        }

        private static void WriteProgramsList(List<InstalledProgram> installedData, string path)
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

        private static void ReadProgramsList(string path)
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
                Console.WriteLine("ReadProgramsList"+ex.Message);
            }
        }

        private static void WMIReceiveEvent()
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

        private static void HandleEvent(object sender, EventArrivedEventArgs e)
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

        private static void CompareAndWriteLog(List<InstalledProgram> newList, List<InstalledProgram> originalList)
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
                        logString = string.Format("事件時間:{0} 事件名稱:{1} 程式名稱:{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), item.EventType, item.Name);
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

    }

    


}
