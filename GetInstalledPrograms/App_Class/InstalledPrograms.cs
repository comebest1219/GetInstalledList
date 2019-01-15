using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;


namespace GetInstalledPrograms.App_Class
{
    public class InstalledProgram : IComparable<InstalledProgram>, IEquatable<InstalledProgram>
    {
        #region "Properties"
        private string _DisplayName = String.Empty;

        /// <summary>
        /// The name that would be displayed in Add/Remove Programs
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                _DisplayName = value;
            }
        }

        private string _Version = String.Empty;

        /// <summary>
        /// The version of the program
        /// </summary>
        public string Version
        {
            get
            {
                return _Version;
            }
            set
            {
                _Version = value;
            }
        }

        private string _InstallDate = String.Empty;

        /// <summary>
        /// The install date of the program
        /// </summary>
        public string InstallDate
        {
            get
            {
                return _InstallDate;
            }
            set
            {
                _InstallDate = value;
            }
        }
        private string _EstimatedSize = String.Empty;
        /// <summary>
        /// The size of the program
        /// </summary>
        public string EstimatedSize
        {
            get
            {
                return _EstimatedSize;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    int.TryParse(value, out int result);
                    _EstimatedSize = (result/1000).ToString()+" MB";
                }
                else
                {
                    _EstimatedSize = value;
                }
                
            }
        }
 
        #endregion

        #region "Constructors"
        public InstalledProgram(string ProgramDisplayName)
        {
            this.DisplayName = ProgramDisplayName;
        }

        public InstalledProgram(string ProgramDisplayName, string ProgramVersion, string ProgramInstallDate, string ProgramEstimatedSize)
        {
            this.DisplayName = ProgramDisplayName;
            this.Version = ProgramVersion;
            this.InstallDate = ProgramInstallDate;
            this.EstimatedSize = ProgramEstimatedSize;
        }
        #endregion


        #region "Public Methods"
        /// <summary>
        /// Retrieves a list of all installed programs on the local computer
        /// </summary>
        public static List<InstalledProgram> GetInstalledPrograms(bool IncludeUpdates=false)
        {
            return InstalledProgram.InternalGetInstalledPrograms(IncludeUpdates, Registry.LocalMachine, Registry.Users);
        }


        /// <summary>
        /// Retrieves a list of all installed programs on the specified computer
        /// </summary>
        /// <param name="ComputerName">The name of the computer to get the list of installed programs from</param>
        /// <param name="IncludeUpdates">Determines whether or not updates for installed programs are included in the list</param>
        /// <returns></returns>
        public static List<InstalledProgram> GetInstalledPrograms(string ComputerName, bool IncludeUpdates)
        {
            try
            {
                return InstalledProgram.InternalGetInstalledPrograms(IncludeUpdates, RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, ComputerName), RegistryKey.OpenRemoteBaseKey(RegistryHive.Users, ComputerName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new List<InstalledProgram>();
            }

        }

        //// Sorting function, required by IComparable interface
        public int CompareTo(InstalledProgram other)
        {
            return string.Compare(this.DisplayName, other.DisplayName);
        }

        //// Equality function, required by IEquatable interface
        public bool Equals(InstalledProgram other)
        {
            if ((this.DisplayName == other.DisplayName) && (this.Version == other.Version))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion


        #region "Private Methods"
        private static List<InstalledProgram> InternalGetInstalledPrograms(bool IncludeUpdates, RegistryKey HklmPath, RegistryKey HkuPath)
        {
            List<InstalledProgram> ProgramList = new List<InstalledProgram>();

            RegistryKey ClassesKey = HklmPath.OpenSubKey("Software\\Classes\\Installer\\Products");

            // ---Wow64 Uninstall key
            RegistryKey Wow64UninstallKey = HklmPath.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            ProgramList = GetUninstallKeyPrograms(Wow64UninstallKey, ClassesKey, ProgramList, IncludeUpdates);

            // ---Standard Uninstall key
            RegistryKey StdUninstallKey = HklmPath.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            ProgramList = GetUninstallKeyPrograms(StdUninstallKey, ClassesKey, ProgramList, IncludeUpdates);

            foreach (string UserSid in HkuPath.GetSubKeyNames())
            {
                // ---HKU Uninstall key
                RegistryKey CuUnInstallKey = HkuPath.OpenSubKey((UserSid + "\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"));
                ProgramList = GetUninstallKeyPrograms(CuUnInstallKey, ClassesKey, ProgramList, IncludeUpdates);
                // ---HKU Installer key
                RegistryKey CuInstallerKey = HkuPath.OpenSubKey((UserSid + "\\Software\\Microsoft\\Installer\\Products"));
                ProgramList = GetUserInstallerKeyPrograms(CuInstallerKey, HklmPath, ProgramList);
            }

            // Close the registry keys
            try
            {
                HklmPath.Close();
                HkuPath.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(("Error closing registry key - " + ex.Message));
            }

            // Sort the list alphabetically and return it to the caller
            ProgramList.Sort();
            return ProgramList;
        }

        private static bool IsProgramInList(string ProgramName, List<InstalledProgram> ListToCheck)
        {
            //InstalledProgram installedProgram = new InstalledProgram(ProgramName);
            return ListToCheck.Contains(new InstalledProgram(ProgramName));
        }

        private static List<InstalledProgram> GetUserInstallerKeyPrograms(RegistryKey CuInstallerKey, RegistryKey HklmRootKey, List<InstalledProgram> ExistingProgramList)
        {
            if (!(CuInstallerKey == null))
            {
                foreach (string CuProductGuid in CuInstallerKey.GetSubKeyNames())
                {
                    bool ProductFound = false;
                    foreach (string UserDataKeyName in HklmRootKey.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData").GetSubKeyNames())
                    {
                        if (!(UserDataKeyName == "S-1-5-18"))
                        {
                            // Ignore the LocalSystem account
                            RegistryKey ProductsKey = HklmRootKey.OpenSubKey(("Software\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\"
                                            + (UserDataKeyName + "\\Products")));
                            if (!(ProductsKey == null))
                            {
                                string[] LmProductGuids = ProductsKey.GetSubKeyNames();
                                foreach (string LmProductGuid in LmProductGuids)
                                {
                                    if ((LmProductGuid == CuProductGuid))
                                    {
                                        RegistryKey UserDataProgramKey = HklmRootKey.OpenSubKey(("Software\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\"
                                                        + (UserDataKeyName + ("\\Products\\"
                                                        + (LmProductGuid + "\\InstallProperties")))));
                                        if (!(int.Parse(UserDataProgramKey.GetValue("SystemComponent", 0).ToString()) == 1))
                                        {
                                            string Name = CuInstallerKey.OpenSubKey(CuProductGuid).GetValue("ProductName", String.Empty).ToString();
                                            string ProgVersion = String.Empty;
                                            try
                                            {
                                                ProgVersion = UserDataProgramKey.GetValue("DisplayVersion", String.Empty).ToString();
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine(ex.Message);
                                            }

                                            if ((!(Name == String.Empty)
                                                        && !InstalledProgram.IsProgramInList(Name, ExistingProgramList)))
                                            {
                                                ExistingProgramList.Add(new InstalledProgram(Name));
                                                ProductFound = true;
                                            }

                                        }
                                        break;
                                    }
                                }

                                if (ProductFound)
                                {
                                    break;
                                }

                                try
                                {
                                    ProductsKey.Close();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            return ExistingProgramList;
        }

        private static List<InstalledProgram> GetUninstallKeyPrograms(RegistryKey UninstallKey, RegistryKey ClassesKey, List<InstalledProgram> ExistingProgramList, bool IncludeUpdates)
        {
            if (!(UninstallKey == null))
            {
                string[] SubKeyNames = UninstallKey.GetSubKeyNames();
                // Loop through all subkeys (each one represents an installed program)
                foreach (string SubKeyName in SubKeyNames)
                {
                    try
                    {
                        RegistryKey CurrentSubKey = UninstallKey.OpenSubKey(SubKeyName);
                        // Skip this program if the SystemComponent flag is set
                        int IsSystemComponent = 0;
                        try
                        {
                            IsSystemComponent = int.Parse(CurrentSubKey.GetValue("SystemComponent", 0).ToString());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                        }

                        if (!(IsSystemComponent == 1))
                        {
                            // If the WindowsInstaller flag is set then add the key name to our list of Windows Installer GUIDs
                            if (!(int.Parse(CurrentSubKey.GetValue("WindowsInstaller", 0).ToString()) == 1))
                            {
                                System.Text.RegularExpressions.Regex WindowsUpdateRegEx = new System.Text.RegularExpressions.Regex("KB[0-9]{6}$");
                                string ProgramReleaseType = CurrentSubKey.GetValue("ReleaseType", String.Empty).ToString();
                                string ProgVersion = String.Empty;
                                string ProgInstallDate = String.Empty;
                                string ProgEstimatedSize = String.Empty;
                                try
                                {
                                    ProgVersion = CurrentSubKey.GetValue("DisplayVersion", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                try
                                {
                                    ProgInstallDate = CurrentSubKey.GetValue("InstallDate", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                try
                                {
                                    ProgEstimatedSize = CurrentSubKey.GetValue("EstimatedSize", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                // Check to see if this program is classed as an update
                                if (((WindowsUpdateRegEx.Match(SubKeyName).Success == true)
                                            || (!(CurrentSubKey.GetValue("ParentKeyName", String.Empty).ToString() == String.Empty)
                                            || ((ProgramReleaseType == "Security Update")
                                            || ((ProgramReleaseType == "Update Rollup")
                                            || (ProgramReleaseType == "Hotfix"))))))
                                {
                                    if (IncludeUpdates)
                                    {
                                        // Add the program to our list if we are including updates in this search
                                        string Name = CurrentSubKey.GetValue("DisplayName", String.Empty).ToString();
                                        if ((!(Name == String.Empty)
                                                    && !InstalledProgram.IsProgramInList(Name, ExistingProgramList)))
                                        {
                                            ExistingProgramList.Add(new InstalledProgram(Name, ProgVersion,ProgInstallDate, ProgEstimatedSize));
                                        }

                                    }

                                }
                                else
                                {
                                    // If not classed as an update
                                    bool UninstallStringExists = false;
                                    foreach (string valuename in CurrentSubKey.GetValueNames())
                                    {
                                        if (string.Equals("UninstallString", valuename, StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            UninstallStringExists = true;
                                            break;
                                        }

                                    }

                                    if (UninstallStringExists)
                                    {
                                        string Name = CurrentSubKey.GetValue("DisplayName", String.Empty).ToString();
                                        if ((!(Name == String.Empty)
                                                    && !InstalledProgram.IsProgramInList(Name, ExistingProgramList)))
                                        {
                                            ExistingProgramList.Add(new InstalledProgram(Name, ProgVersion, ProgInstallDate, ProgEstimatedSize));
                                        }

                                    }

                                }

                            }
                            else
                            {
                                // If WindowsInstaller
                                string ProgVersion = String.Empty;
                                string ProgInstallDate = String.Empty;
                                string ProgEstimatedSize = String.Empty;
                                string Name = String.Empty;

                                try
                                {
                                    string MsiKeyName = InstalledProgram.GetInstallerKeyNameFromGuid(SubKeyName);
                                    RegistryKey CrGuidKey = ClassesKey.OpenSubKey(MsiKeyName);
                                    if (!(CrGuidKey == null))
                                    {
                                        Name = CrGuidKey.GetValue("ProductName", String.Empty).ToString();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                try
                                {
                                    ProgVersion = CurrentSubKey.GetValue("DisplayVersion", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }

                                try
                                {
                                    ProgInstallDate = CurrentSubKey.GetValue("InstallDate", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                try
                                {
                                    ProgEstimatedSize = CurrentSubKey.GetValue("EstimatedSize", String.Empty).ToString();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                                }

                                if ((!(Name == String.Empty) && !InstalledProgram.IsProgramInList(Name, ExistingProgramList)))
                                {
                                    ExistingProgramList.Add(new InstalledProgram(Name, ProgVersion, ProgInstallDate, ProgEstimatedSize));
                                }

                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine((SubKeyName + (" - " + ex.Message)));
                    }

                }

                // Close the registry key
                try
                {
                    UninstallKey.Close();
                    UninstallKey.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }

            return ExistingProgramList;
        }


        private static string GetInstallerKeyNameFromGuid(string GuidName)
        {
            string[] MsiNameParts = GuidName.Replace("{", "").Replace("}", "").Split('-');
            System.Text.StringBuilder MsiName = new System.Text.StringBuilder();
            // Just reverse the first 3 parts
            for (int i = 0; (i <= 2); i++)
            {
                MsiName.Append(InstalledProgram.ReverseString(MsiNameParts[i]));
            }

            // For the last 2 parts, reverse each character pair
            for (int j = 3; (j <= 4); j++)
            {
                for (int i = 0; (i
                            <= (MsiNameParts[j].Length - 1)); i++)
                {
                    MsiName.Append(MsiNameParts[j][(i + 1)]);
                    MsiName.Append(MsiNameParts[j][i]);
                    i++;
                }

            }

            return MsiName.ToString();
        }

        private static string ReverseString(string input)
        {
            char[] Chars = input.ToCharArray();
            Array.Reverse(Chars);
            return new string(Chars);
        }

        #endregion
    }
}
