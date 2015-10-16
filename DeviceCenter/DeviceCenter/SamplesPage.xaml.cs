using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace DeviceCenter
{
    public class AppInformation
    {
        public AppInformation(string posterFile, string title, string description)
        {
            this.PosterFile = posterFile;
            this.Title = title;
            this.Description = description;
        }

        public string PosterFile { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
    }

    /// <summary>
    /// Interaction logic for SamplesPage.xaml
    /// </summary>
    public partial class SamplesPage : Page
    {
        public SamplesPage()
        {
            InitializeComponent();

            App.TelemetryClient.TrackPageView(this.GetType().Name);

            this.AppList.Add(new AppInformation(
                "Assets/cloud.png",
                Strings.Strings.SamplesBlinkyTitle,
                Strings.Strings.SamplesBlinkyMessage1 + "\n" + Strings.Strings.SamplesBlinkyMessage2));

            this.AppList.Add(new AppInformation(
                "Assets/cloud.png",
                Strings.Strings.SamplesRadioTitle,
                Strings.Strings.SamplesRadioMessage1));

            this.listViewApps.ItemsSource = this.AppList;
        }

        private ObservableCollection<AppInformation> AppList = new ObservableCollection<AppInformation>();
    }
}
