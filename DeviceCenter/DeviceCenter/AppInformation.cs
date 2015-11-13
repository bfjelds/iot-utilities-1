using DeviceCenter.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace DeviceCenter
{
    /// <summary>
    /// TBD
    /// </summary>
    public class AppInformation
    {
        ~AppInformation()
        {
            DiscoveryHelper.Release();
        }

        // TBD: Warning
        // If we change the samples we need to make sure to update the Package Full Name
        // strings bellow. Failing to so so will cause the app to not be updated because
        // Device Center will think the app is already deployed.

        public static string Blinky_PackageFullName_x86 = "BlinkyHeadedWebService_1.0.2.0_x86__4a1rdwapkt3r8";
        public static string Blinky_PackageFullName_arm = "BlinkyHeadedWebService_1.0.2.0_arm__4a1rdwapkt3r8";

        public static string InternetRadio_PackageFullName_x86 = "InternetRadioHeaded_1.0.1.0_x86__4a1rdwapkt3r8";
        public static string InternetRadio_PackageFullName_arm = "InternetRadioHeaded_1.0.1.0_arm__4a1rdwapkt3r8";

        public string PosterFile { get; private set; }

        public string Title { get; private set; }

        public string AppName { get; private set; }

        public string AppPort { get; private set; }

        public string Screenshot { get; private set; }

        public string Description { get; private set; }

        public class ApplicationFiles
        {
            public FileInfo AppX { get; set; }

            public string PackageFullName { get; set; }

            public FileInfo Certificate { get; set; }

            public List<FileInfo> Dependencies { get; set; }
        }
        
        public Dictionary<string, ApplicationFiles> PlatformFiles { get; private set; }

        internal static string CachedRootDirectory = null;

        internal static FileInfo MakePath(string fileName)
        {
            if (CachedRootDirectory == null)
                CachedRootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (CachedRootDirectory != null)
                return new FileInfo(Path.Combine(CachedRootDirectory, "Assets", "Apps", fileName));

            return null;
        }

        private DiscoveryHelper _discoveryHelper = DiscoveryHelper.Instance;

        public ObservableCollection<DiscoveredDevice> ConfiguredDevices
        {
            get
            {
                return _discoveryHelper.ConfiguredDevices;
            }
        }

        public string OnlineInfo { get; private set; }

        public string OnlineSourceCode { get; private set; }

        public override string ToString()
        {
            return this.AppName;
        }

        public static ObservableCollection<AppInformation> Initialize()
        {
            if (AppList.Count == 0)
            {
                AppList.Add(new AppInformation()
                {
                    AppName = "BlinkyHeadedWebService",
                    AppPort = "8000",
                    PosterFile = "Assets/Blinky.png",
                    Screenshot = "Assets/BlinkyScreenshot.png",
                    Title = Strings.Strings.SamplesBlinkyTitle,
                    Description = Strings.Strings.SamplesBlinkyMessage1 + "\n" + Strings.Strings.SamplesBlinkyMessage2,
                    PlatformFiles = new Dictionary<string, ApplicationFiles>()
                    {
                        {
                            "x86", new ApplicationFiles()
                            {
                                AppX = MakePath("Blinky\\x86\\BlinkyHeadedWebService_1.0.2.0_x86.appx"),
                                PackageFullName = Blinky_PackageFullName_x86,
                                Certificate = MakePath("Blinky\\x86\\BlinkyHeadedWebService_1.0.2.0_x86.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    MakePath("Dependencies\\X86\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    MakePath("Dependencies\\X86\\Microsoft.VCLibs.x86.14.00.appx")
                                }
                            }
                        },
                        {
                            "ARM", new ApplicationFiles()
                            {
                                AppX = MakePath("Blinky\\arm\\BlinkyHeadedWebService_1.0.2.0_ARM.appx"),
                                PackageFullName = Blinky_PackageFullName_arm,
                                Certificate = MakePath("Blinky\\arm\\BlinkyHeadedWebService_1.0.2.0_ARM.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    MakePath("Dependencies\\ARM\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    MakePath("Dependencies\\ARM\\Microsoft.VCLibs.ARM.14.00.appx")
                                }
                            }
                        }
                    },
                    OnlineInfo = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm",
                    OnlineSourceCode = "http://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm"
                });

                AppList.Add(new AppInformation()
                {
                    AppName = "InternetRadioHeaded",
                    AppPort = "8001",
                    PosterFile = "Assets/Radio.png",
                    Screenshot = "Assets/RadioScreenshot.png",
                    Title = Strings.Strings.SamplesRadioTitle,
                    Description = Strings.Strings.SamplesRadioMessage1,
                    PlatformFiles = new Dictionary<string, ApplicationFiles>()
                    {
                        {
                            "x86", new ApplicationFiles()
                            {
                                AppX = MakePath("InternetRadio\\x86\\InternetRadioHeaded_1.0.1.0_x86.appx"),
                                PackageFullName = InternetRadio_PackageFullName_x86, 
                                Certificate = MakePath("InternetRadio\\X86\\InternetRadioHeaded_1.0.1.0_x86.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    MakePath("Dependencies\\X86\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    MakePath("Dependencies\\X86\\Microsoft.VCLibs.x86.14.00.appx")
                                }
                            }
                        },
                        {
                            "ARM", new ApplicationFiles()
                            {
                                AppX = MakePath("InternetRadio\\ARM\\InternetRadioHeaded_1.0.1.0_ARM.appx"),
                                PackageFullName = InternetRadio_PackageFullName_arm,
                                Certificate = MakePath("InternetRadio\\ARM\\InternetRadioHeaded_1.0.1.0_ARM.cer"),
                                Dependencies = new List<FileInfo>()
                                {
                                    MakePath("Dependencies\\ARM\\Microsoft.NET.Native.Runtime.1.1.appx"),
                                    MakePath("Dependencies\\ARM\\Microsoft.VCLibs.ARM.14.00.appx")
                                }
                            }
                        }
                    },
                    OnlineInfo = "https://www.hackster.io/windows-iot/connected-clock-radio-85c9fc",
                    OnlineSourceCode = "https://www.hackster.io/windows-iot/connected-clock-radio-85c9fc"
                });
            }

            return AppList;
        }

        private static readonly ObservableCollection<AppInformation> AppList = new ObservableCollection<AppInformation>();
    }
}
