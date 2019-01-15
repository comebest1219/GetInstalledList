using GetInstalledPrograms.App_Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetInstalledPrograms.Model
{
    public class InstalledListDataLog
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Size { get; set; }
        public string InstallDate { get; set; }
        public string EventType { get; set; }

        public InstalledListDataLog(InstalledProgram installedProgram, string eventType)
        {
            Name = installedProgram.DisplayName;
            Version = installedProgram.Version;
            Size = installedProgram.EstimatedSize;
            InstallDate = installedProgram.InstallDate;
            EventType = eventType;
        }
    }
}
