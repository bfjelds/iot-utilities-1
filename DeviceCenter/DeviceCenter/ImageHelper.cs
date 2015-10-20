// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;

namespace DeviceCenter
{
    public class DriveInfo
    {
        public DriveInfo(string driveName, string physicalDriveId, ulong size, string model)
        {
            DriveName = driveName;
            PhysicalDriveId = physicalDriveId;
            Size = size;
            Model = model;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} [{2}]", this.DriveName, this.SizeString, this.Model);
        }

        public string DriveName { get; private set; }
        public string PhysicalDriveId { get; private set; }

        private ulong size;
        public ulong Size
        {
            get
            {
                return size;
            }

            private set
            {
                size = value;
                InitSizeString();
            }
        }

        public string Model { get; private set; }

        public string SizeString { get; private set; }

        private readonly string[] unitString = { "bytes", "Kb", "Mb", "Gb" };

        private void InitSizeString()
        {
            var s = size;
            int unit = 0;
            while (s > 1024 && unit < 3)
            {
                s /= 1024;
                ++unit;
            }

            SizeString = s.ToString() + unitString[unit];
        }


        static ManagementEventWatcher usbwatcher = null;

        public static void AddRemoveUSBHandler(EventArrivedEventHandler USBRemoved)
        {            
            string query = "SELECT * FROM __InstanceDeletionEvent WITHIN 10 WHERE TargetInstance ISA \"Win32_LogicalDisk\"";            
            try
            {
                usbwatcher = new ManagementEventWatcher(new EventQuery(query));
                usbwatcher.EventArrived += USBRemoved;
                usbwatcher.Start();
            }

            catch (Exception)
            {
                if (usbwatcher != null)
                    usbwatcher.Stop();
            }
        }


        public static void AddInsertUSBHandler(EventArrivedEventHandler USBAdded)
        {
            string query = "SELECT * FROM __InstanceCreationEvent WITHIN 10 WHERE TargetInstance ISA \"Win32_LogicalDisk\"";
            
            try
            {            
                usbwatcher = new ManagementEventWatcher(new EventQuery(query));
                usbwatcher.EventArrived += USBAdded;
                usbwatcher.Start();
            }

            catch (Exception)
            {                
                if (usbwatcher != null)
                    usbwatcher.Stop();
            }
        }       

        static public List<DriveInfo> GetRemovableDriveList()
        {
            var res = new List<DriveInfo>();
            try
            {
                var drives = new ManagementClass("Win32_DiskDrive");                
                var moc = drives.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    try
                    {
                        var mediaType = (string)mo["MediaType"];
                        if (!mediaType.ToLowerInvariant().Contains("removable"))
                        {
                            continue;
                        }
                        var partitions = mo.GetRelated("Win32_DiskPartition");
                        foreach (ManagementObject partition in partitions)
                        {
                            var logicalDisks = partition.GetRelated("Win32_LogicalDisk");
                            if (logicalDisks.Count != 1)
                            {
                                continue;
                            }
                            var logicalDisk = logicalDisks.Cast<ManagementObject>().First();
                            var diskName = (string)logicalDisk["Name"];
                            var deviceId = (string)mo["DeviceId"];
                            var size = (ulong)mo["Size"];
                            var model = (string)mo["Model"];

                            res.Add(new DriveInfo(diskName, deviceId, size, model));
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            return res;
        }

    }

    public class Dism
    {
        public static Process FlashFFUImageToDrive(string ffuImage, DriveInfo driveInfo)
        {
            /*
            // TODO (alecont): Add logic to pick up dism from system32 if available...
            var current_dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var dism = System.IO.Path.Combine(current_dir, @"dism\dism.exe");
            */

            var dismExe = Environment.SystemDirectory + "\\" + "dism.exe";

            Process process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas";
            process.StartInfo.FileName = dismExe;
            process.StartInfo.Arguments =
                String.Format("/Apply-Image /ApplyDrive:{0} /SkipPlatformCheck /ImageFile:{1}",                
                    driveInfo.PhysicalDriveId,
                    ffuImage);
            System.Diagnostics.Debug.WriteLine("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            // TBD make this async and cancellable.
            process.Start();

            return process;
        }
    }

}
