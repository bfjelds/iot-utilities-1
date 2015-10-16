using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

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

            this.AppList.Add(new AppInformation(
                "Assets/Blinky.png",
                Strings.Strings.SamplesBlinkyTitle,
                Strings.Strings.SamplesBlinkyMessage1 + "\n" + Strings.Strings.SamplesBlinkyMessage2));

            this.AppList.Add(new AppInformation(
                "Assets/Radio.png",
                Strings.Strings.SamplesRadioTitle,
                Strings.Strings.SamplesRadioMessage1));

            this.listViewApps.ItemsSource = this.AppList;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private ObservableCollection<AppInformation> AppList = new ObservableCollection<AppInformation>();
    }
}
