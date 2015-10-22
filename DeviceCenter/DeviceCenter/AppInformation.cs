using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace DeviceCenter
{
    public class AppInformation
    {
        public string PosterFile { get; private set; }
        public string Title { get; private set; }
        public string Screenshot { get; private set; }
        public string Description { get; private set; }

        public class ApplicationFiles
        {
            public FileInfo AppX { get; set; }
            public FileInfo Certificate { get; set; }
            public List<FileInfo> Dependencies { get; set; }
        }
        
        public Dictionary<string, ApplicationFiles> PlatformFiles { get; private set; }

        public string OnlineInfo { get; private set; }
        public string OnlineSourceCode { get; private set; }
        public static ObservableCollection<AppInformation> Initialize()
        {
            if (_appList.Count == 0)
            {
                _appList.Add(new AppInformation()
                {
                    PosterFile = "Assets/Blinky.png",
                    Screenshot = "Assets/BlinkyScreenshot.png",
                    Title = Strings.Strings.SamplesBlinkyTitle,
                    Description = Strings.Strings.SamplesBlinkyMessage1 + "\n" + Strings.Strings.SamplesBlinkyMessage2,
                    PlatformFiles = new Dictionary<string, ApplicationFiles>()
                    {
                        {
                            "x86", new ApplicationFiles()
                            {
                                AppX = new FileInfo("Blinky\\x86\\BlinkyHeadedWebService_1.0.1.0_x86.appx"),
                                Certificate = new FileInfo("Blinky\\x86\\BlinkyHeadedWebService_1.0.1.0_x86.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    new FileInfo("Dependencies\\X86\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    new FileInfo("Dependencies\\X86\\Microsoft.VCLibs.x86.14.00.appx")
                                }
                            }
                        },
                        {
                            "arm", new ApplicationFiles()
                            {
                                AppX = new FileInfo("Blinky\\arm\\BlinkyHeadedWebService_1.0.1.0_ARM.appx"),
                                Certificate = new FileInfo("Blinky\\arm\\BlinkyHeadedWebService_1.0.1.0_ARM.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    new FileInfo("Dependencies\\ARM\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    new FileInfo("Dependencies\\ARM\\Microsoft.VCLibs.ARM.14.00.appx")
                                }
                            }
                        }
                    },
                    OnlineInfo = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm",
                    OnlineSourceCode = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm"
                });

                _appList.Add(new AppInformation()
                {
                    PosterFile = "Assets/Radio.png",
                    Screenshot = "Assets/RadioScreenshot.png",
                    Title = Strings.Strings.SamplesRadioTitle,
                    Description = Strings.Strings.SamplesRadioMessage1,
                    PlatformFiles = new Dictionary<string, ApplicationFiles>()
                    {
                        {
                            "x86", new ApplicationFiles()
                            {
                                AppX = new FileInfo("InternetRadio\\x86\\InternetRadioHeaded_1.0.1.0_x86.appx"),
                                Certificate = new FileInfo("InternetRadio\\X86\\InternetRadioHeaded_1.0.1.0_x86.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    new FileInfo("Dependencies\\X86\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    new FileInfo("Dependencies\\X86\\Microsoft.VCLibs.x86.14.00.appx")
                                }
                            }
                        },
                        {
                            "arm", new ApplicationFiles()
                            {
                                AppX = new FileInfo("InternetRadio\\ARM\\InternetRadioHeaded_1.0.1.0_ARM.appx"),
                                Certificate = new FileInfo("InternetRadio\\ARM\\InternetRadioHeaded_1.0.1.0_ARM.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    new FileInfo("Dependencies\\ARM\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    new FileInfo("Dependencies\\ARM\\Microsoft.VCLibs.ARM.14.00.appx")
                                }
                            }
                        }
                    },
                    OnlineInfo = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm",
                    OnlineSourceCode = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm"
                });
            }

            return _appList;
        }

        private static ObservableCollection<AppInformation> _appList = new ObservableCollection<AppInformation>();
    }
}
