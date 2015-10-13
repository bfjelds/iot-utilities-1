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
        /// <summary>
        /// Parser for the LKG file.   
        /// TBD this is needed only for internal build.
        /// </summary>
        LastKnownGood lkg = new LastKnownGood();


        public SetupDevicePage()
        {
            InitializeComponent();
            lkg.ReadFile();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDriveList();
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
        private void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            WindowWarning dlg = new WindowWarning()
            {
                Header = Strings.Strings.ConnectAlertTitle,
                Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2
            };

            bool? confirmation = dlg.ShowDialog();
            if (confirmation.HasValue && confirmation.Value)
            {
                DriveInfo driveInfo = (RemoveableDevicesComboBox.SelectedItem as ListBoxItem).Tag as DriveInfo;

                if (driveInfo == null)
                {
                    // TBD some message that there's no drive.
                }

                // TBD hardcoded images for now.        
                string mbmFFUFile = "\\\\winbuilds\\release\\TH2_Release\\10565.0.151006-2014\\x86fre\\Images\\IoTUAP\\MBM_1024x768\\Test\\en-us\\Flash.ffu";
                string rpi2FFUFile = "\\\\winbuilds\\release\\TH2_Release\\10565.0.151006-2014\\woafre\\Images\\IoTUAP\\RPi2_1024x768\\Production\\en-us\\Flash.ffu";

                // Pick up the Device Type.
                string deviceType = ComboBoxDeviceType.Tag.ToString();
                string ffuName;

                // determine the ffu to be used for SD flash
                if (deviceType == "MBM")
                {
                    ffuName = mbmFFUFile;
                }
                else if (deviceType == "RPi2")
                {
                    ffuName = rpi2FFUFile;
                }
                else
                {
                    ffuName = null;
                }

                if (string.IsNullOrEmpty(ffuName))
                {
                    // TBD no file found, err message.
                }

                // Flash it.
                Dism.FlashFFUImageToDrive(ffuName, driveInfo);
            }
        }
    }
}
