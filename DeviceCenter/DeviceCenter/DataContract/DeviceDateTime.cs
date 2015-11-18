using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.DataContract
{
    public class CurrentDateTime
    {
        [DataMember(Name = "Current")]
        public DeviceDateTime Current { get; set; }
    }

    public class DeviceDateTime
    {
        [DataMember(Name = "Day")]
        public int Day { get; set; }

        [DataMember(Name = "Month")]
        public int Month { get; set; }

        [DataMember(Name = "Year")]
        public int Year { get; set; }

        [DataMember(Name = "Hour")]
        public int Hour { get; set; }

        [DataMember(Name = "Minute")]
        public int Minute { get; set; }

        [DataMember(Name = "Second")]
        public int Second { get; set; }

        public DateTime? DateTime
        {
            get
            {
                try
                {
                    DateTime? ret = new DateTime(Year, Month, Day, Hour, Minute, Second);

                    return ret;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    return null;
                }
            }
        }
    }
}
