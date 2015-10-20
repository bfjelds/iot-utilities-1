using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class AvailableNetworks
    {
        [DataMember(Name = "AvailableNetworks")]
        public AvailableNetwork[] Items { get; set; }

        public override string ToString()
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append("====== AvailableNetworks ======");
            sbMessage.AppendLine();
            foreach (var n in Items)
            {
                sbMessage.AppendFormat("[{0}] - [{1}]", n.SSID, n.IsAlreadyConnected);
                sbMessage.AppendLine();
            }
            sbMessage.Append("===============================");
            sbMessage.AppendLine();

            return sbMessage.ToString();
        }        
    }

    [DataContract]
    public class AvailableNetwork
    {
        [DataMember(Name = "AlreadyConnected")]
        public bool IsAlreadyConnected { get; set; }
        [DataMember(Name = "SSID")]
        public string SSID { get; set; }
        [DataMember(Name = "SecurityEnabled")]
        public bool SecurityEnabled { get; set; }
        [DataMember(Name = "SignalQuality")]
        public int SignalQuality { get; set; }
    }
}
