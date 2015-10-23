using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class IoTProcesses
    {
        [DataMember(Name = "Processes")]
        public IoTProcess[] Items { get; set; }
    }

    [DataContract]
    public class IoTProcess
    {
        [DataMember(Name = "AppName")]
        public string AppName { get; set; }

        [DataMember(Name = "CPUUsage")]
        public string CPUUsage { get; set; }

        [DataMember(Name = "ImageName")]
        public string ImageName { get; set; }

        [DataMember(Name = "PackageFullName")]
        public string PackageFullName { get; set; }

        [DataMember(Name = "PageFileUsage")]
        public string PageFileUsage { get; set; }

        [DataMember(Name = "PrivateWorkingSet")]
        public string PrivateWorkingSet { get; set; }

        [DataMember(Name = "ProcessId")]
        public string ProcessId { get; set; }

        [DataMember(Name = "Publisher")]
        public string Publisher { get; set; }

        [DataMember(Name = "SessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "TotalCommit")]
        public string TotalCommit { get; set; }

        [DataMember(Name = "UserName")]
        public string UserName { get; set; }

        [DataMember(Name = "Version")]
        public string Version { get; set; }

        [DataMember(Name = "VirtualSize")]
        public string VirtualSize { get; set; }

        [DataMember(Name = "WorkingSetSize")]
        public string WorkingSetSize { get; set; }
    }
}
