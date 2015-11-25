﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.IO;

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
            return $"{this.DriveName} {this.SizeString} [{this.Model}]";
        }

        public string DriveName { get; private set; }

        public string PhysicalDriveId { get; private set; }

        private ulong _size;

        public ulong Size
        {
            get
            {
                return _size;
            }

            private set
            {
                _size = value;
                InitSizeString();
            }
        }

        public string Model { get; private set; }

        public string SizeString { get; private set; }

        private readonly string[] _unitString = { "bytes", "Kb", "Mb", "Gb" };

        private void InitSizeString()
        {
            var s = _size;
            var unit = 0;
            while (s > 1024 && unit < 3)
            {
                s /= 1024;
                ++unit;
            }

            SizeString = s.ToString() + _unitString[unit];
        }


        static ManagementEventWatcher _usbWatcher = null;        

        static public void InitializeWatcher()
        {
            var query = "SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA \'Win32_LogicalDisk\'";
            _usbWatcher = new ManagementEventWatcher(new EventQuery(query));            
        }

        static public void DisposeWatcher()
        {
            _usbWatcher?.Dispose();  
        }

        static public void AddUSBDetectionHandler(EventArrivedEventHandler usbDetectionHandler)
        {
            try
            {
                _usbWatcher.EventArrived += usbDetectionHandler;
                _usbWatcher.Start();
            }

            catch (Exception)
            {
                _usbWatcher?.Stop();
            }                       
        }

        static public void RemoveUSBDetectionHandler()
        {
            _usbWatcher?.Stop();            
        }

        static public List<DriveInfo> GetRemovableDriveList()
        {
            var res = new List<DriveInfo>();
            try
            {
                var drives = new ManagementClass("Win32_DiskDrive");                
                var moc = drives.GetInstances();
                foreach (var mo in moc.Cast<ManagementObject>())
                {
                    try
                    {
                        if (mo["MediaType"] == null)
                            continue;

                        var mediaType = (string)mo["MediaType"];
                        if (!mediaType.ToLowerInvariant().Contains("removable"))
                        {
                            continue;
                        }
                        var partitions = mo.GetRelated("Win32_DiskPartition");
                        foreach (var o in partitions)
                        {
                            var partition = (ManagementObject) o;
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
                        // do nothing, return empty list
                    }
                }
            }
            catch (Exception)
            {
                // do nothing, return empty list
            }

            return res;
        }

    }

    public class Dism
    {
        public static Process FlashFfuImageToDrive(string ffuImage, DriveInfo driveInfo)
        {
            // rely on DISM in system32.
            var dismExe = Environment.SystemDirectory + "\\" + "dism.exe";

            // Bail if FFU file does not exist.
            if (!File.Exists(ffuImage))
            {
                Debug.WriteLine("Dism: ffu file not found: [{0}]", ffuImage);
                throw new FileNotFoundException(null, ffuImage);
            }

            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    FileName = dismExe,
                    Arguments =
                        $"/Apply-Image /ApplyDrive:{driveInfo.PhysicalDriveId} /SkipPlatformCheck /ImageFile:\"{ffuImage}\""
                }
            };

            System.Diagnostics.Debug.WriteLine("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            
            process.Start();

            return process;
        }
    }

}
