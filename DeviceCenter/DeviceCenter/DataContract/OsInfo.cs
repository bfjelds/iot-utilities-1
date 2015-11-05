using System.Runtime.Serialization;

namespace DeviceCenter.DataContract
{
    public class OsInfo
    {
        [DataMember(Name = "ComputerName")]
        public string ComputerName { get; set; }

        [DataMember(Name = "Language")]
        public string Language { get; set; }

        [DataMember(Name = "OsEdition")]
        public string OsEdition { get; set; }

        [DataMember(Name = "OsVersion")]
        public string OsVersion { get; set; }

        [DataMember(Name = "Platform")]
        public string Platform { get; set; }

        public string Arch
        {
            get
            {
                if(string.IsNullOrEmpty(OsVersion))
                {
                    return string.Empty;
                }

                if(OsVersion.Contains("arm"))
                {
                    return "ARM";
                }
                else if(OsVersion.Contains("x86"))
                {
                    return "x86";
                }
                else
                {
                    return string.Empty;
                }
            }

        }
    }
}
