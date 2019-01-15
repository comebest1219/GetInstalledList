using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_Class
{
    public class AppLib
    {

        public static void WriteToFile(string Message, string path, bool isCreate=true)
        {
            try
            {
                FileMode mode = (isCreate)? FileMode.Create: (File.Exists(path))? FileMode.Append:FileMode.Create;
                using (FileStream fs = new FileStream(path, mode))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(Message);
                    }
                }      
            }
            catch (Exception e)
            {
                Console.WriteLine("WriteFileError"+e.Message);
            }           
        }



        public static string ReadFromFile(string path)
        {
            string sLine = string.Empty;
            if (!File.Exists(path)) return sLine;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    using (StreamReader sw = new StreamReader(fs))
                    {
                        sLine = sw.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReadFileError"+e.Message);
            }

            return sLine;
        }
    }
}
