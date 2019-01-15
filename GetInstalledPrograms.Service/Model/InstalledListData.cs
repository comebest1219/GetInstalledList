using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class InstalledListData
    {
        public string DisplayName { get; set; }
        public string Version { get; set; }        
        public string InstallDate { get; set; }
        public string EstimatedSize { get; set; }

        public InstalledListData(string name, string version, string size, string installDate)
        {
            DisplayName = name;
            Version = version;           
            InstallDate = installDate;
            EstimatedSize = size;
        }
    }
}
