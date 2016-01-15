﻿using System.Runtime.Serialization;

namespace IotCoreAppDeployment
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
