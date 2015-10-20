using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class IPConfigurations
    {
        [DataMember(Name = "Adapters")]
        public IPConfiguration[] Items { get; set; }

        public override string ToString()
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append("====== WirelessAdapters ======");
            sbMessage.AppendLine();
            foreach (var i in Items)
            {
                sbMessage.AppendFormat("[{0}] - [{1}]", i.Name, i.Description);
                sbMessage.AppendLine();
            }
            sbMessage.Append("===============================");
            sbMessage.AppendLine();

            return sbMessage.ToString();
        }
    }

    [DataContract]
    public class IPConfiguration
    {
        [DataMember(Name = "Description")]
        public string Description { get; set; }
        [DataMember(Name = "HardwareAddress")]
        public string HardwareAddress { get; set; }
        [DataMember(Name = "Index")]
        public int Index { get; set; }
        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "Type")]
        public string Type { get; set; }
        [DataMember(Name = "IpAddresses")]
        public IpAddress[] IPAddresses { get; set; }
    }

    [DataContract]
    public class IpAddress
    {
        [DataMember(Name = "IpAddress")]
        public string IP { get; set; }
    }
}