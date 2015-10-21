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
        public AppxPackage(string appxFileName, string certFileName)
        {
            string appxPath = @"";
            appxPath += appxFileName;

            string certPath = @"";
            certPath += certFileName;

            //try
            //{
            //    using (StreamReader sr = File.OpenText(path))
            //    {

            //    }
            //}
        }

        /// <summary>
        /// The appx file for the package to install.
        /// </summary>
        [DataMember]
        //public string AppxFileName { get; set; }
        public byte[] AppxFileContent { get; set; }

        /// <summary>
        /// The certificate file for the package to install.
        /// </summary>
        [DataMember]
        //public string CertFileName { get; set; }
        public byte[] CertFileContent { get; set; }
    }
}
