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

    public enum BuildPathType
    {
        HttpURL,
        ISOFile,
        MSIFile,
        FFUFile,
        InvalidPath
    }

    public class ExtractFFUProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
    }

    public class FlashingCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
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
        public event EventHandler<ExtractFFUProgressEventArgs> ExtractFFUProgress;
        public event EventHandler<FlashingCompletedEventArgs> FlashingCompleted; 
        #endregion

        #region Private Members

        private readonly Object _dismLock = new Object();
        private Process _dismProcess = null;
        private FlashingStates _currentFlashingState = FlashingStates.Completed;

        // Singleton
        private static DeviceSetupHelper _instance;

        #endregion

        #region Telemetry related info

        public string DeviceType = "";
        public string Build = "";

        private double _flashStartTime = 0;
        private DriveInfo _cachedDriveInfo;

        #endregion

        public static DeviceSetupHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DeviceSetupHelper();
                return _instance;
            }
        }

        public FlashingStates CurrentFlashingState
        {
            get { return _currentFlashingState; }
            set { _currentFlashingState = value; }
        }

        #region Helper Methods to extract ISO

        protected virtual void OnExtractFFUProgress(ExtractFFUProgressEventArgs e)
        {
            EventHandler<ExtractFFUProgressEventArgs> handler = ExtractFFUProgress;
            if (null != handler)
            {
                handler(this, e);
            }
        }

        public string ExtractFFUFromISO(string isoFilePath, LkgPlatform lkgPlatform)
        {
            var ExtractFFUProgressArgs = new ExtractFFUProgressEventArgs();
            ExtractFFUProgressArgs.Progress = 0;
            OnExtractFFUProgress(ExtractFFUProgressArgs);

            string msiName;
            string ffuPath;
            switch (lkgPlatform.Platform)
            {
                case "MBM":
                    msiName = _mbmMsiName;
                    ffuPath = _mbmFfuSubPath;
                    break;
                case "RPi2":
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
                ExtractFFUProgressArgs = new ExtractFFUProgressEventArgs();
                ExtractFFUProgressArgs.Progress = 33;
                OnExtractFFUProgress(ExtractFFUProgressArgs);

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
                ExtractFFUProgressArgs = new ExtractFFUProgressEventArgs();
                ExtractFFUProgressArgs.Progress = 66;
                OnExtractFFUProgress(ExtractFFUProgressArgs);

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
                ExtractFFUProgressArgs = new ExtractFFUProgressEventArgs();
                ExtractFFUProgressArgs.Progress = 100;
                OnExtractFFUProgress(ExtractFFUProgressArgs);

                return msiProcess.ExitCode != 0 ? string.Empty : Path.Combine(extractionPath, ffuPath);
            }
        }
        public void DisMountIso(string isoFilePath)
        {
            using (var powerShellInstance = PowerShell.Create())
            {
                powerShellInstance.AddScript("$dismountResult = Dismount-DiskImage " + isoFilePath + ";");
                var psOutput = powerShellInstance.Invoke();
            }
        }

        #endregion

        #region Helper Methods to Flash FFU to SD Card
        
        public int FlashFFU(string ffuPath, DriveInfo driveInfo)
        {
            lock (_dismLock)
            {
                Debug.Assert(driveInfo != null);

                // Track for telemetry
                _cachedDriveInfo = driveInfo;
                _flashStartTime = App.GlobalStopwatch.ElapsedMilliseconds;

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
                    { "DeviceType", (DeviceType != null) ? DeviceType : "" },
                    { "Build",  (Build != null) ? Build : ""},
                    { "DriveSize", (_cachedDriveInfo != null) ? _cachedDriveInfo.SizeString : ""},
                    { "DriveModel", (_cachedDriveInfo != null) ? _cachedDriveInfo.Model : "" }
                });

                bool fSuccess = _dismProcess.ExitCode == 0 ? true : false;
                   
                if (_dismProcess != null)
                {
                    _dismProcess.Dispose();
                    _dismProcess = null;
                }

                var args = new FlashingCompletedEventArgs();
                args.Success = fSuccess;
                OnFlashingCompleted(args);
            }
        }
        protected virtual void OnFlashingCompleted(FlashingCompletedEventArgs e)
        {
            EventHandler<FlashingCompletedEventArgs> handler = FlashingCompleted;
            if (null != handler)
            {
                handler(this, e);
            }
        }
        public void CancelDism()
        {
            lock (_dismLock)
            {
                if (_dismProcess != null)
                {
                    _dismProcess.Kill();
                    App.TelemetryClient.TrackEvent("FlashSDCardCancel");
                }
            }
        }

        #endregion

        #region Static Methods
        public static BuildPathType GetTypeOfBuildPath(string BuildPath)
        {
            if (BuildPath.StartsWith("http://") || BuildPath.StartsWith("https://"))
            {
                return BuildPathType.HttpURL;
            }
            else
            {
                var fileExtension = Path.GetExtension(BuildPath);
                if (fileExtension.Equals(".iso", StringComparison.InvariantCultureIgnoreCase))
                    return BuildPathType.ISOFile;
                else if (fileExtension.Equals(".msi", StringComparison.InvariantCultureIgnoreCase))
                    return BuildPathType.MSIFile;
                else if (fileExtension.Equals(".ffu", StringComparison.InvariantCultureIgnoreCase))
                    return BuildPathType.FFUFile;
                else 
                    return BuildPathType.InvalidPath;
            }
        }
        #endregion
    };
}

