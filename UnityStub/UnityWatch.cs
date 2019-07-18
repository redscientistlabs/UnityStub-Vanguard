using Newtonsoft.Json;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.NetCore.StaticTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;
using UnityStub;

namespace UnityStub
{
    public static class UnityWatch
    {
        public static string UnityStubVersion = "0.0.5";
        public static string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static UnityStubFileInfo currentFileInfo = new UnityStubFileInfo();

        public static bool stubInterfaceEnabled = false;


        public static void Start()
        {

            if (VanguardCore.vanguardConnected)
                RemoveDomains();

            DisableInterface();

            RtcCore.EmuDirOverride = true; //allows the use of this value before vanguard is connected


            string backupPath = Path.Combine(UnityWatch.currentDir, "FILEBACKUPS");
            string paramsPath = Path.Combine(UnityWatch.currentDir, "PARAMS");

            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            if (!Directory.Exists(paramsPath))
                Directory.CreateDirectory(paramsPath);

            string disclaimerPath = Path.Combine(currentDir, "LICENSES", "DISCLAIMER.TXT");
            string disclaimerReadPath = Path.Combine(currentDir, "PARAMS", "DISCLAIMERREAD");

            if (File.Exists(disclaimerPath) && !File.Exists(disclaimerReadPath))
            {
                MessageBox.Show(File.ReadAllText(disclaimerPath).Replace("[ver]", UnityWatch.UnityStubVersion), "Unity Stub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.Create(disclaimerReadPath);
            }

            //If we can't load the dictionary, quit the wgh to prevent the loss of backups
            if (!FileInterface.LoadCompositeFilenameDico(UnityWatch.currentDir))
                Application.Exit();

        }

        private static void RemoveDomains()
        {
            if (currentFileInfo.targetInterface != null)
            {
                currentFileInfo.targetInterface.CloseStream();
                currentFileInfo.targetInterface = null;
            }

            UpdateDomains();
        }

        public static bool RestoreTarget()
        {
            bool success = false;
            if (UnityWatch.currentFileInfo.autoUncorrupt)
            {
                if (StockpileManager_EmuSide.UnCorruptBL != null)
                {
                    StockpileManager_EmuSide.UnCorruptBL.Apply(false);
                    success = true;
                }
                else
                {
                    //CHECK CRC WITH BACKUP HERE AND SKIP BACKUP IF WORKING FILE = BACKUP FILE
                   success = UnityWatch.currentFileInfo.targetInterface.ResetWorkingFile();
                }
            }
            else
            {
                success = UnityWatch.currentFileInfo.targetInterface.ResetWorkingFile();
            }

            return success;
        }

        internal static bool LoadTarget()
        {

            FileInterface.identity = FileInterfaceIdentity.HASHED_PREFIX;

            string filename = null;

            OpenFileDialog OpenFileDialog1;
            OpenFileDialog1 = new OpenFileDialog();

            OpenFileDialog1.Title = "Select the Game Executable";
            OpenFileDialog1.Filter = "Executable Files|*.exe";
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (OpenFileDialog1.FileName.ToString().Contains('^'))
                {
                    MessageBox.Show("You can't use a file that contains the character ^ ");
                    return false;
                }

                filename = OpenFileDialog1.FileName;
            }
            else
                return false;

            if (!CloseTarget(false))
                return false;

            FileInfo unityExeFile = new FileInfo(filename);

            UnityWatch.currentFileInfo.targetShortName = unityExeFile.Name;
            UnityWatch.currentFileInfo.targetFullName = unityExeFile.FullName;

            DirectoryInfo unityFolder = unityExeFile.Directory;

            var allFiles = DirSearch(unityFolder.FullName).ToArray();

            if (allFiles.FirstOrDefault(it => it.ToUpper().Contains("UNITY")) == null)
            {
                MessageBox.Show("Could not find unity files");
                return false;
            }

            var allDllFiles = allFiles.Where(it => it.ToUpper().EndsWith(".DLL")).ToArray();
            var allUnityDllFiles = allDllFiles.Where(it => it.ToUpper().Contains("UNITY")).ToArray();
            var unityEngineDll = allDllFiles.Where(it => it.ToUpper().Contains("UNITYENGINE.DLL")).ToArray();


            List<string> targetFiles = new List<string>();

            switch (UnityWatch.currentFileInfo.selectedTargetType)
            {
                case TargetType.UNITYEXE:
                    targetFiles.Add(unityExeFile.FullName);
                    break;
                case TargetType.UNITYEXE_ALLDLL:
                    targetFiles.Add(unityExeFile.FullName);
                    targetFiles.AddRange(allDllFiles);
                    break;
                case TargetType.UNITYEXE_UNITYDLL:
                    targetFiles.Add(unityExeFile.FullName);
                    targetFiles.AddRange(allUnityDllFiles);
                    break;
                case TargetType.UNITYEXE_KNOWNDLL:
                    targetFiles.Add(unityExeFile.FullName);

                    var allKnownGames = allDllFiles.Where(it => 
                    it.ToUpper().Contains("PHYSICS") ||
                    it.ToUpper().Contains("CLOTH") ||
                    it.ToUpper().Contains("ANIMATION") ||
                    it.ToUpper().Contains("PARTICLE") ||
                    it.ToUpper().Contains("TERRAIN") ||
                    it.ToUpper().Contains("VEHICLES") ||
                    it.ToUpper().Contains("UNITYENGINE.DLL")
                    ).ToArray();

                    targetFiles.AddRange(allKnownGames);

                    break;
                case TargetType.ALLTHEGAME:
                    targetFiles.AddRange(allFiles);
                    break;
                case TargetType.UNITYENGINE:
                    targetFiles.AddRange(unityEngineDll);
                    break;
            }

            string multipleFiles = "";

            for (int i = 0; i < targetFiles.Count; i++)
            {
                multipleFiles += targetFiles[i];

                if (i < targetFiles.Count - 1)
                    multipleFiles += "|";
            }

            var mfi = new MultipleFileInterface(multipleFiles, UnityWatch.currentFileInfo.bigEndian, UnityWatch.currentFileInfo.useAutomaticBackups);

            if (UnityWatch.currentFileInfo.useCacheAndMultithread)
                mfi.getMemoryDump();

            UnityWatch.currentFileInfo.targetInterface = mfi;

            Executor.unityExeFile = unityExeFile.FullName;

            StockpileManager_EmuSide.UnCorruptBL = null;

            if (VanguardCore.vanguardConnected)
                UnityWatch.UpdateDomains();

            return true;
        }

        private static List<String> DirSearch(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }

            return files;
        }

        internal static void KillProcess()
        {

            if (currentFileInfo.TerminateBeforeExecution && Executor.unityExeFile != null)
            {

                string otherProgramShortFilename = Path.GetFileName(Executor.unityExeFile);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "taskkill";
                startInfo.Arguments = $"/IM \"{otherProgramShortFilename}\"";
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                Process processTemp = new Process();
                processTemp.StartInfo = startInfo;
                processTemp.EnableRaisingEvents = true;
                try
                {
                    processTemp.Start();
                    processTemp.WaitForExit();
                }
                catch (Exception ex)
                {
                    throw ex;
                }


            }
        }
        internal static bool CloseTarget(bool updateDomains = true)
        {
            if (UnityWatch.currentFileInfo.targetInterface != null)
            {
                if (!UnityWatch.RestoreTarget())
                {
                    MessageBox.Show("Unable to restore the backup. Aborting!");
                    return false;
                }
                    
                UnityWatch.currentFileInfo.targetInterface.CloseStream();
                UnityWatch.currentFileInfo.targetInterface = null;
            }

            if (updateDomains)
                UpdateDomains();
            return true;
        }

        public static void UpdateDomains()
        {
            try
            {
                PartialSpec gameDone = new PartialSpec("VanguardSpec");
                gameDone[VSPEC.SYSTEM] = "Unity";
                gameDone[VSPEC.GAMENAME] = UnityWatch.currentFileInfo.targetShortName;
                gameDone[VSPEC.SYSTEMPREFIX] = "UnityStub";
                gameDone[VSPEC.SYSTEMCORE] = "UnityStub";
                //gameDone[VSPEC.SYNCSETTINGS] = BIZHAWK_GETSET_SYNCSETTINGS;
                gameDone[VSPEC.OPENROMFILENAME] = currentFileInfo.targetFullName;
                gameDone[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = new string[0];
                gameDone[VSPEC.MEMORYDOMAINS_INTERFACES] = GetInterfaces();
                gameDone[VSPEC.CORE_DISKBASED] = false;
                AllSpec.VanguardSpec.Update(gameDone);

                //This is local. If the domains changed it propgates over netcore
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_DOMAINSUPDATED, true, true);

                //Asks RTC to restrict any features unsupported by the stub
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_RESTRICTFEATURES, true, true);

            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();
            }
        }

        public static MemoryDomainProxy[] GetInterfaces()
        {
            try
            {
                Console.WriteLine($" getInterfaces()");
                if (currentFileInfo.targetInterface == null)
                {
                    Console.WriteLine($"rpxInterface was null!");
                    return new MemoryDomainProxy[] { };
                }

                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();


                foreach (var fi in (currentFileInfo.targetInterface as MultipleFileInterface).FileInterfaces)
                    interfaces.Add(new MemoryDomainProxy(fi));

                foreach (MemoryDomainProxy mdp in interfaces)
                    mdp.BigEndian = currentFileInfo.bigEndian;

                return interfaces.ToArray();
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();

                return new MemoryDomainProxy[] { };
            }

        }

        public static void EnableInterface()
        {
            S.GET<StubForm>().btnResetBackup.Enabled = true;
            S.GET<StubForm>().btnRestoreBackup.Enabled = true;

            stubInterfaceEnabled = true;
        }
        public static void DisableInterface()
        {
            S.GET<StubForm>().btnResetBackup.Enabled = false;
            S.GET<StubForm>().btnRestoreBackup.Enabled = false;
            stubInterfaceEnabled = false;
        }

    }


}
