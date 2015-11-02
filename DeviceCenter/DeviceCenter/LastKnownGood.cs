// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System;
using System.Threading.Tasks;

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
    public class LkgPlatform
    {
        public LkgPlatform()
        {
            this.LkgBuilds = new List<BuildInfo>();
        }

        public static LkgPlatform CreateMbm()
        {
            var result = new LkgPlatform {Platform = "MBM"};
            return result;
        }

        public static LkgPlatform CreateRpi2()
        {
            var result = new LkgPlatform {Platform = "RPi2"};
            return result;
        }

        public static LkgPlatform CreateQCom()
        {
            var result = new LkgPlatform {Platform = "QCOM"};
            return result;
        }

        public const string MbmName = "Minnowboard MAX";
        public const string RaspberryPi2Name = "Raspberry Pi 2";
        public const string DragonboardName = "Qualcomm DragonBoard 410c";

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
                case "QCOM":
                    return DragonboardName;
            }
            
            return string.Empty;
        }

        /// <summary>
        /// List of LKG builds or none.
        /// </summary>
        [DataMember]
        public List<BuildInfo> LkgBuilds { get; set; }
    }

    /// <summary>
    /// LKG (last known good) info.
    /// </summary>
    [DataContract]
    public class LkgAllPlatforms
    {
        [DataMember]
        public List<LkgPlatform> AllPlatforms;
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
        public LkgAllPlatforms LkgAllPlatforms { get; private set; }

        /// <summary>
        /// Deserialize info in json.        
        /// </summary>
        public async Task<bool> ReadFileAsync()
        {
            var fileExists = false;

            await Task.Factory.StartNew(() =>
            {
                fileExists = File.Exists(LkgFileName);

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

                if (fileExists)
                {
                    try
                    {
                        string fileContent;
                        using (var sr = File.OpenText(LkgFileName))
                        {
                            fileContent = sr.ReadToEnd();
                        }

                        var jsonSerializer = new DataContractJsonSerializer(typeof(LkgAllPlatforms));

                        LkgAllPlatforms = (LkgAllPlatforms)jsonSerializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            });

            if (!fileExists)
            {
                Debug.WriteLine("LkgInsider: LKG file not found");
            }

            return fileExists;
        }
    }
}
