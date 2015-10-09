using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for SetupDevicePage.xaml
    /// </summary>
    public partial class SetupDevicePage : Page
    {
        public SetupDevicePage()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDriveList();
        }

        private void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            Storyboard animation = (Storyboard)FindResource("StoryboardShowMessage");
            animation.Begin();
        }

        private void RefreshDriveList()
        {
            var drives = DriveInfo.GetRemovableDriveList();
            RemoveableDevicesComboBox.Items.Clear();
            
            if (drives.Count == 0)
            {
                StatusMessageBox.Text = "No SD cards found. Please insert one and press the refresh button.";
                var item = new ListBoxItem();
                item.Content = "No SD cards found";
                RemoveableDevicesComboBox.Items.Add(item);
                RemoveableDevicesComboBox.SelectedIndex = -1;
                RemoveableDevicesComboBox.IsEnabled = false;
            }
            else
            {
                foreach (var drive in drives)
                {
                    StatusMessageBox.Text = "";
                    var item = new ListBoxItem();
                    item.Content = String.Format("{0} {1} [{2}]", drive.DriveName, drive.SizeString, drive.Model);
                    item.Tag = drive;
                    RemoveableDevicesComboBox.Items.Add(item);
                    RemoveableDevicesComboBox.SelectedIndex = 0;
                    RemoveableDevicesComboBox.IsEnabled = true;
                }
            }
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            Storyboard animation = (Storyboard)FindResource("StoryboardHideMessage");
            animation.Begin();
            // Do the actual flashing here 
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Storyboard animation = (Storyboard)FindResource("StoryboardHideMessage");
            animation.Begin();
        }
    }
}
