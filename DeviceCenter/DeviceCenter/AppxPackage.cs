using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter
{
    [DataContract]
    class AppxPackage
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PackageFamilyName { get; set; }

        [DataMember]
        public string PackageFullName { get; set; }

        [DataMember]
        public string PackageOrigin { get; set; }

        [DataMember]
        public string PackageRelativeId { get; set; }
    }
}
