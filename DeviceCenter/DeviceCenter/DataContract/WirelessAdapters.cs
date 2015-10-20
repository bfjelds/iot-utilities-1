using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    [DataContract]
    public class WirelessAdapters
    {
        [DataMember(Name = "Interfaces")]
        public Interface[] Items { get; set; }

        public override string ToString()
        {
            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append("====== Wireless Adapters ======");
            sbMessage.AppendLine();
            foreach (var i in Items)
            {
                sbMessage.AppendFormat("[{0}] - [{1}]", i.Description, i.GUID);
                sbMessage.AppendLine();
            }
            sbMessage.Append("===============================");
            sbMessage.AppendLine();

            return sbMessage.ToString();
        }
    }

    [DataContract]
    public class Interface
    {
        [DataMember(Name = "Description")]
        public string Description { get; set; }
        [DataMember(Name = "GUID")]
        public string GUID { get; set; }
        [DataMember(Name = "Index")]
        public int Index { get; set; }
        [DataMember(Name = "ProfilesList")]
        public string[] ProfilesList { get; set; }
    }
}
