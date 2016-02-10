using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{

    [DataContract]
    public class IoTProcessesTh2
    {
        [DataMember(Name = "Processes")]
        public IoTProcessTh2[] Items { get; set; }
    }

    [DataContract]
    public class IoTProcessTh2
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

    [DataContract]
    public class IoTProcessesRs1
    {
        [DataMember(Name = "Processes")]
        public IoTProcessRs1[] Items { get; set; }
    }

    [DataContract]
    public class IoTProcessVersion
    {
        [DataMember(Name = "Version")]
        public String Build { get; set; }
        public String Major { get; set; }
        public String Minor { get; set; }
        public String Revision { get; set; }
    }

    public class IoTProcessRs1
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
        public IoTProcessVersion Version { get; set; }

        [DataMember(Name = "VirtualSize")]
        public string VirtualSize { get; set; }

        [DataMember(Name = "WorkingSetSize")]
        public string WorkingSetSize { get; set; }
    }
}
