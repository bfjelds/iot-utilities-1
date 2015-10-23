using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class InstalledPackages
    {
        [DataMember(Name = "InstalledPackages")]
        public AppxPackage[] Items { get; set; }
    }


    [DataContract]
    public class AppxPackage
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "PackageFamilyName")]
        public string PackageFamilyName { get; set; }

        [DataMember(Name = "PackageFullName")]
        public string PackageFullName { get; set; }

        [DataMember(Name = "PackageOrigin")]
        public string PackageOrigin { get; set; }

        [DataMember(Name = "PackageRelativeId")]
        public string PackageRelativeId { get; set; }
    }
}
