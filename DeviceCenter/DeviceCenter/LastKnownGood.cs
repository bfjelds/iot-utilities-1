// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System;

namespace DeviceCenter
{
    /// <summary>
    /// Build info of a lkg.
    /// </summary>
    [DataContract]
    public class BuildInfo
    {
        public BuildInfo(int buildNumber, string buildPath) { Build = buildNumber; Path = buildPath; }

        /// <summary>
        /// Build number
        /// </summary>
        [DataMember]
        public int Build { get; set; }

        public override string ToString()
        {
            return Build.ToString();
        }

        /// <summary>
        /// Build path
        /// </summary>
        [DataMember]
        public string Path { get; set; }
    }
    
    /// <summary>
    /// Each platform may have multiple LKGs.
    /// </summary>
    [DataContract]
    public class LKGPlatform
    {
        public const string MbmName = "Minnowboard MAX";
        public const string RaspberryPi2Name = "Raspberry Pi 2";

        /// <summary>
        /// E.g. "MBM", "RPi2", etc.
        /// </summary>
        [DataMember]
        public string Platform { get; set; }

        public override string ToString()
        {
            switch (this.Platform)
            {
                case "MBM":
                    return MbmName;
                case "RPi2":
                    return RaspberryPi2Name;
            }

            System.Diagnostics.Debug.Fail("Unsupported platforms should not show in this list");
            return null;
        }

        /// <summary>
        /// List of LKG builds or none.
        /// </summary>
        [DataMember]
        public List<BuildInfo> LkgBuilds;
    }

    /// <summary>
    /// LKG (last known good) info.
    /// </summary>
    [DataContract]
    public class LKGAllPlatforms
    {
        [DataMember]
        public List<LKGPlatform> AllPlatforms;
    }    
    
    /// <summary>
    /// Parse the LKG info file.
    /// </summary>
    public class LastKnownGood
    {
        /// <summary>
        /// LKG info file.  tbd point to the final file.
        /// </summary>
        static string LkgFileName = "\\\\webnas\\AthensDrop\\LKG\\iot_lkg.txt";
        
        // tbd - remove this.
        // static readonly string LkgFileName = "c:\\temp\\iot_lkg.txt";

        /// <summary>
        /// Deserialized contents.
        /// </summary>
        public LKGAllPlatforms LkgAllPlatforms { get; private set; }
        
        /// <summary>
        /// Deserialize info in json.        
        /// </summary>
        public void ReadFile()
        {
            if (!File.Exists(LkgFileName))
            {
                Debug.WriteLine("LkgInsider: LKG file not found");
                throw new FileNotFoundException();
            }

            //  LKG file looks like the following:
            //
            //  {
            //      "AllPlatforms":
            //      [
            //          { 
            //              "Platform":"MBM",
            //              "LkgBuilds":
            //              [
            //                  {"Build":1,"Path":"path1"},
            //                  {"Build":2,"Path":"path2"},
            //                  {"Build":3,"Path":"path3"}
            //              ]            
            //          },            
            //          {
            //              "Platform":"RPi2",
            //              "LkgBuilds":
            //              [
            //                  {"Build":4,"Path":"path4"},
            //                  {"Build":5,"Path":"path5"},
            //                  {"Build":6,"Path":"path6"}
            //              ]            
            //          },
            //          {
            //              "Platform":"QCOM",
            //              "LkgBuilds":
            //              [
            //                  {"Build":7,"Path":"path7"},
            //                  {"Build":8,"Path":"path8"},
            //                  {"Build":9,"Path":"path9"}
            //              ]            
            //          }
            //      ]
            //  }

            try
            {
                string fileContent;
                using (var sr = File.OpenText(LkgFileName))
                {
                    fileContent = sr.ReadToEnd();
                }

                var jsonSerializer = new DataContractJsonSerializer(typeof(LKGAllPlatforms));

                LkgAllPlatforms = (LKGAllPlatforms)jsonSerializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
