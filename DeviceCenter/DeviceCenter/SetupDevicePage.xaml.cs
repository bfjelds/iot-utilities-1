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
            ErrorMessage.Visibility = Visibility.Visible; 
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

        /// <summary>
        /// Called when user clicks Continue to flash image to SD card. 
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            DriveInfo driveInfo = (RemoveableDevicesComboBox.SelectedItem as ListBoxItem).Tag as DriveInfo;

            if (driveInfo == null)
            {
                // TBD some message that there's no drive.
            }
         
            // TBD image selection here.                 
            string mbmFFUFile = "\\\\winbuilds\\release\\TH2_Release\\10565.0.151006-2014\\x86fre\\Images\\IoTUAP\\MBM_1024x768\\Test\\en-us\\Flash.ffu";
            string rpi2FFUFile = "\\\\winbuilds\\release\\TH2_Release\\10565.0.151006-2014\\woafre\\Images\\IoTUAP\\RPi2_1024x768\\Production\\en-us\\Flash.ffu";

            bool bArm = true;

            string ffuName;
            // Pick up the Device Type.
            if (bArm == true)
            {
                ffuName = rpi2FFUFile;
            }
            else
            {
                ffuName = mbmFFUFile;
            }

            // Flash it.
            Dism.FlashFFUImageToDrive(ffuName, driveInfo);
           
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Hidden;
        }
    }
}
