using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace DeviceCenter.Helper
{
    public enum FlashingStates
    {
        Downloading,
        Extracting,
        Flashing,
        Completed
    }
    class DeviceSetupHelper
    {
        #region Constants

        private readonly string _rpi2MsiName = "Windows_10_IoT_Core_RPi2.msi";
        private readonly string _mbmMsiName = "Windows_10_IoT_Core_Mbm.msi";
        private readonly string _mbmFfuSubPath = @"Microsoft IoT\FFU\MinnowBoardMax\Flash.ffu";
        private readonly string _rpi2FfuSubPath = @"Microsoft IoT\FFU\RaspberryPi2\Flash.ffu";

        #endregion

        #region Events
        public delegate void ExtractFFUProgressEventHandler(int progress);
        public event ExtractFFUProgressEventHandler ExtractFFUProgress;

        public delegate void FlashingCompletedEventHandler(bool success);
        public event FlashingCompletedEventHandler FlashingCompleted; 
        #endregion

        #region Private Members

        private readonly Object _dismLock = new Object();
        private Process _dismProcess = null;

        #endregion

        #region Telemetry related info

        private double _flashStartTime = 0;
        private LkgPlatform _cachedDeviceType;
        private DriveInfo _cachedDriveInfo;
        private BuildInfo _cachedBuildInfo;

        #endregion

        #region Helper Methods to extract ISO

        public string ExtractFFUFromISO(string isoFilePath, LkgPlatform lkgPlatform)
        {
            string msiName;
            string ffuPath;
            switch (lkgPlatform.DeviceType)
            {
                case DeviceTypes.MBM:
                    msiName = _mbmMsiName;
                    ffuPath = _mbmFfuSubPath;
                    break;
                case DeviceTypes.RPI2:
                    msiName = _rpi2MsiName;
                    ffuPath = _rpi2FfuSubPath;
                    break;
                default:
                    throw new NotSupportedException();
            }

            using (PowerShell powerShellInstance = PowerShell.Create())
            {
                // use "AddScript" to add the contents of a script file to the end of the execution pipeline.
                // use "AddCommand" to add individual commands/cmdlets to the end of the execution pipeline.
                powerShellInstance.AddScript("$mountResult = Mount-DiskImage " + isoFilePath + " -PassThru | Get-Volume; $mountResult.DriveLetter; ");

                // invoke the script
                Collection<PSObject> psOutput = powerShellInstance.Invoke();

                // Update the progress to 1/3
                ExtractFFUProgress(33);

                // loop through each output object item
                string driveLetter = "";
                foreach (PSObject outputItem in psOutput)
                {
                    if (null != outputItem)
                    {
                        driveLetter = outputItem.BaseObject.ToString();
                        break;
                    }
                }

                string msiPath;
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    msiPath = driveLetter + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + msiName;
                }
                else
                {
                    DisMountIso(isoFilePath);
                    return string.Empty;
                }

                string extractionPath = Path.Combine(Path.GetTempPath() + "IoTCoreMSIContent");

                // Delete everything in the extractionPath
                try
                {
                    var extractionPathInfo = new DirectoryInfo(extractionPath);
                    foreach (var file in extractionPathInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (var dir in extractionPathInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    //Its ok if the directory is not present
                }

                // Update the extraction progress to 2/3 done
                ExtractFFUProgress(66);

                var msiProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = "msiexec",
                        Arguments =
                            $@"/a {msiPath} /qn TARGETDIR={extractionPath} REINSTALLMODE=amus"
                    }
                };

                // msiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                msiProcess.Start();
                msiProcess.WaitForExit();
                DisMountIso(isoFilePath);

                // At this point, we are done with Extraction, change the progress to completed
                ExtractFFUProgress(100);

                return msiProcess.ExitCode != 0 ? string.Empty : Path.Combine(extractionPath, ffuPath);
            }
        }
        public void DisMountIso(string isoFilePath)
        {
            using (var powerShellInstance = PowerShell.Create())
            {
                // Dismount the ISO
                powerShellInstance.AddScript("$mountResult = Dismount-DiskImage " + isoFilePath);
                var psOutput = powerShellInstance.Invoke();
            }
        }

        #endregion

        #region Helper Methods to Flash FFU to SD Card

        public int FlashFFU(BuildInfo bldInfo, LkgPlatform deviceType, DriveInfo driveInfo)
        {
            int processsId = FlashFFU(bldInfo.Path, driveInfo);

            App.TelemetryClient.TrackEvent("FlashSDCard", new Dictionary<string, string>()
            {
                { "DeviceType", (deviceType != null) ? deviceType.ToString() : "" },
                { "Build",  (bldInfo != null) ? bldInfo.Build.ToString() : ""}
            });

            // For flash speed metric telemetry
            _cachedBuildInfo = bldInfo;
            lock (_dismLock)
            {
                _cachedDeviceType = deviceType;
            }
            _cachedDriveInfo = driveInfo;
            _flashStartTime = App.GlobalStopwatch.ElapsedMilliseconds;
            return processsId;
        }

        public int FlashFFU(string ffuPath, DriveInfo driveInfo)
        {
            lock (_dismLock)
            {
                Debug.Assert(driveInfo != null);
                   _dismProcess = Dism.FlashFfuImageToDrive(ffuPath, driveInfo);
                   _dismProcess.EnableRaisingEvents = true;
                   _dismProcess.Exited += DismProcess_Exited;

               
                return _dismProcess.Id;
            }
        }

        private void DismProcess_Exited(object sender, EventArgs e)
        {
            lock (_dismLock)
            {
                // Measure how long it took to flash the image
                App.TelemetryClient.TrackMetric("FlashSDCardTimeMs", App.GlobalStopwatch.ElapsedMilliseconds - _flashStartTime, new Dictionary<string, string>()
                {
                    { "DeviceType", (_cachedDeviceType != null) ? _cachedDeviceType.ToString() : "" },
                    { "Build",  (_cachedBuildInfo != null) ? _cachedBuildInfo.Build.ToString() : ""},
                    { "DriveSize", (_cachedDriveInfo != null) ? _cachedDriveInfo.SizeString : ""},
                    { "DriveModel", (_cachedDriveInfo != null) ? _cachedDriveInfo.Model : "" }
                });

                bool fSuccess = _dismProcess.ExitCode == 0 ? true : false;
                   
                if (_dismProcess != null)
                {
                    _dismProcess.Dispose();
                    _dismProcess = null;
                }

                FlashingCompleted(fSuccess);
            }
        }

        public void CancelDism()
        {
            lock (_dismLock)
            {
                if (_dismProcess != null)
                {
                    NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_BREAK_EVENT, (uint)_dismProcess.Id);
                    App.TelemetryClient.TrackEvent("FlashSDCardCancel");
                }
            }
        }

        #endregion
    };
}