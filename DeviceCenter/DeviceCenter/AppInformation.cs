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

        public FileInfo AppX { get; private set; }
        public FileInfo Certificate { get; private set; }
        public List<FileInfo> Dependencies { get; private set; }

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
                    AppX = new FileInfo("Blinky.appx"),
                    Certificate = new FileInfo("Blinky.cer"),
                    Dependencies = new List<FileInfo>()
                {
                    new FileInfo("runtime1.appx"),
                    new FileInfo("runtime2.appx")
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
                    AppX = new FileInfo("Radio.appx"),
                    Certificate = new FileInfo("Radio.cer"),
                    Dependencies = new List<FileInfo>()
                {
                    new FileInfo("runtime1.appx"),
                    new FileInfo("runtime2.appx")
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
