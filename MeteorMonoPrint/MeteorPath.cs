using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using Ttp.Meteor;

namespace MeteorInkjet
{
    /// <summary>
    /// Object which deals with locating the Meteor API components.
    /// 
    /// There are two situations which commonly arise:
    /// 
    ///  (1) None of the components can be found
    ///  (2) Only the managed components can be found
    /// 
    /// Situation #1 should be the normal situation in a Meteor SDK installation,
    /// unless the Meteor installation folder has been added to system paths.
    /// 
    /// Situation #2 arises if Visual Studio has copied the referenced assemblies
    /// locally (controlled by the "Copy Local" property in the assembly reference)
    /// 
    /// To locate managed components, a handler for the AppDomain's AssemblyResolve
    /// event is installed.
    /// 
    /// If unmanaged components are in a different folder to the managed PrinterInterfaceCLS, 
    /// the path to the installed Meteor PrinterInterface library is added to the local 
    /// environment path.
    ///
    /// For both of the above, the 32-bit path (API\x86) or the 64-bit path (API\amd64) 
    /// is selected depending on whether the current application is 32-bit or 64-bit.
    /// </summary>
    class MeteorPath
    {
        /// <summary>
        /// Attempt to locate the Meteor PrinterInterface.  If it cannot be loaded using the default 
        /// environment path, the installed Meteor API folder is added to the local environment path.
        /// Returns true if successful.
        /// </summary>
        static public bool LocatePrinterInterface() {
            // Install handler for an unresolved assembly.
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            // Now call the handler method.  This extra call is necessary because if we call into 
            // PrinterInterfaceCLS directly in the LocatePrinterInterface method, the assmebly load happens 
            // before we get a chance to install the AssemblyResolve event
            return DoLocatePrinterInterface();
        }

        /// <summary>
        /// Retrieve the Meteor API path from the registry.
        /// As of Meteor r18496, this can be found under CurrentUser, which avoids the Windows 
        /// 32 bit / 64 bit registry indirection of LocalMachine keys.
        /// 
        /// The installation keys are found in
        ///    Software\MeteorInkjet\Install 64 bit (for 64 bit installations)
        ///    Software\MeteorInkjet\Install 32 bit (for 32 bit installations)
        /// </summary>
        private static string GetMeteorAPIPath() {
            // If we're running as a 64 bit application, we need the 64 bit install.
            // If we're running as a 32 bit application, either a 32 bit or a 64 bit install will do.
            // If we find multiple Meteor installations, go for the most recent revision.
            bool runningAs32Bit = IntPtr.Size == 4;
            string installDir = null;
            int revision = -1;
            for (UInt32 i = 0; i <= (runningAs32Bit ? 1 : 0); i++) {
                string keyName = string.Format("Software\\Meteor Inkjet\\Install {0} bit", (i == 0) ? 64 : 32);
                try {
                    using (RegistryKey installKey = Registry.CurrentUser.OpenSubKey(keyName)) {
                        if (installKey != null) {
                            foreach (string subKeyName in installKey.GetSubKeyNames()) {
                                int thisRevision;
                                string[] fields = subKeyName.Split('.');
                                if ( (fields.Length != 4) || !int.TryParse(fields[3], out thisRevision) ) {
                                    Console.WriteLine(string.Format("MeteorPath: Invalid Meteor version key {0}", subKeyName));
                                } else {
                                    using (RegistryKey versionKey = installKey.OpenSubKey(subKeyName)) {
                                        object o = versionKey.GetValue("InstallPath");
                                        if ( o == null ) {
                                            Console.WriteLine(string.Format("MeteorPath: Missing InstallPath for Meteor version {0}", subKeyName));
                                        } else if ( !(o is string) ) {
                                            Console.WriteLine(string.Format("MeteorPath: Invalid InstallPath format for Meteor version {0}", subKeyName));
                                        } else {
                                            string dir = o as string;
                                            Console.WriteLine(string.Format("MeteorPath: Found Meteor installation version = {0} path = '{1}'", subKeyName, dir));
                                            if (thisRevision > revision) {
                                                thisRevision = revision;
                                                installDir = dir;
                                            }    
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("MeteorPath: Failed to access registry: " + e.Message);
                }
            }
            // Adjust the path to include the 32 or 64 bit Meteor API folder
            if (installDir != null) {
                return installDir + (runningAs32Bit ? @"Api\x86" : @"Api\amd64");
            } else {
                return null;
            }
        }

        /// <summary>
        /// List of the Meteor assemblies we expect to load
        /// </summary>
        static List<string> MeteorAssemblies = new List<string>{
            "PrinterInterfaceCLS", "MeteorCLS", "UtilityClasses"
        };

        /// <summary>
        /// Handler called if a required assembly can't be found.
        /// Look in the registry for the Meteor installation path, and if this leads us
        /// to the required file, try loading it using the resulting full path.
        /// </summary>
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            AssemblyName Name = new AssemblyName(args.Name);
            string BaseName = Name.Name;
            if (MeteorAssemblies.Find(x => string.Compare(x, BaseName, true) == 0) != null) {
                string path = GetMeteorAPIPath();
                if (path != null) {
                    path += "\\" + BaseName + ".dll";
                    if (File.Exists(path)) {
                        Console.WriteLine("MeteorPath: Loading " + path);
                        Assembly a = Assembly.LoadFrom(path);
                        return a;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// See if we can access PrinterInterfaceCLS.  If not, look in the registry and see where Meteor
        /// is installed, add this to the path, and try again.
        /// </summary>
        static private bool DoLocatePrinterInterface() {
            // See if we can call into the Printer Interface.  This will drill down into the
            // underlying unmanaged PrinterInterface.dll.
            try {
                PrinterInterfaceCLS.PiGetBuildNumber();
            }

            // If both the managed (PrinterInterfaceCLS.dll) and unmanaged (PrinterInterface.dll) components
            // were found, there's nothing to do.  If just the managed component is found, we must add the
            // API folder to the environment path of the current process.
            catch (DllNotFoundException) {
                string apipath = GetMeteorAPIPath();
                if (apipath != null) {
                    string envpath = Environment.GetEnvironmentVariable("PATH");
                    Console.WriteLine("MeteorPath: Adding '" + apipath + "' to local environment PATH");
                    envpath = apipath + ";" + envpath;
                    Environment.SetEnvironmentVariable("PATH", envpath);

                    // Try again
                    try {
                        PrinterInterfaceCLS.PiGetBuildNumber();
                    }
                    catch (Exception e) {
                        Console.WriteLine("MeteorPath: Exception + " + e.Message);
                        return false;
                    }

                } else {
                    Console.WriteLine("MeteorPath: Install folder not found");
                    return false;
                }
            }

            // Unhandled exception
            catch (Exception e) {
                Console.WriteLine("MeteorPath: Exception + " + e.Message);
                return false;
            }

            return true;
        }
    }
}
