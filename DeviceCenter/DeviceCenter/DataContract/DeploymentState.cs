using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class DeploymentState
    {
        [DataMember(Name = "Code")]
        public int HResult { get; set; }

        [DataMember(Name = "CodeText")]
        public string CodeText { get; set; }

        [DataMember(Name = "Reason")]
        public string Reason { get; set; }

        [DataMember(Name = "Success")]
        public bool IsSuccess { get; set; }
    }
}
