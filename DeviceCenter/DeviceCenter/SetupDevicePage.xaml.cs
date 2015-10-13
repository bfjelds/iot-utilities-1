using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ReadLkgFile();
            RefreshDriveList();
        }

        private async void ReadLkgFile()
        {
            List<LKGPlatform> entries = new List<LKGPlatform>();

            await Task.Run((Action)(() =>
            {
                lkg.ReadFile();

                foreach (var currentPlatform in lkg.lkgAllPlatforms.AllPlatforms)
                {
                    switch (currentPlatform.Platform)
                    {
                        case "MBM":
                            entries.Add(currentPlatform);
                            break;
                        case "RPi2":
                            entries.Add(currentPlatform);
                            break;
                    }
                }
            }));

            foreach (var currentEntry in entries)
                ComboBoxDeviceType.Items.Add(currentEntry);

            if (ComboBoxDeviceType.Items.Count > 0)
                ComboBoxDeviceType.SelectedIndex = 0;

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private async void RefreshDriveList()
        {
            List<DriveInfo> drives = null;
            await Task.Run((Action)(() =>
            {
                drives = DriveInfo.GetRemovableDriveList();
            }));

            if (drives != null)
            {
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
                        RemoveableDevicesComboBox.Items.Add(drive);
                        RemoveableDevicesComboBox.SelectedIndex = 0;
                        RemoveableDevicesComboBox.IsEnabled = true;
                    }
                }
            }

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private Process dismProcess = null;

        /// <summary>
        /// Called when user clicks Continue to flash image to SD card. 
        /// </summary>
        /// <param name="sender">not used</param>
        /// <param name="e">not used</param>
        private void FlashSDCard_Click(object sender, RoutedEventArgs e)
        {
            lock (dismLock)
            {
                if (!UpdateStartState())
                    return;

                DriveInfo driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;
                Debug.Assert(driveInfo != null);

                WindowWarning dlg = new WindowWarning()
                {
                    Header = Strings.Strings.ConnectAlertTitle,
                    Message = Strings.Strings.NewDeviceAlertMessage + "\n" + Strings.Strings.NewDeviceAlertMessage2
                };

                bool? confirmation = dlg.ShowDialog();
                if (confirmation.HasValue && confirmation.Value)
                {
                    // Flash it.
                    BuildInfo build = ComboBoxIotBuild.SelectedItem as BuildInfo;
                    Debug.Assert(build != null);

                    Process dismProcess = Dism.FlashFFUImageToDrive(build.Path, driveInfo);

                    dismProcess.Exited += DismProcess_Exited;
                }
            }
        }

        private object dismLock = new object();

        private void DismProcess_Exited(object sender, EventArgs e)
        {
            lock(dismLock)
            {
                dismProcess.Dispose();
                dismProcess = null;
            }
        }

        private void buttonCancelDism_Click(object sender, RoutedEventArgs e)
        {
            lock (dismLock)
            {
                if (dismProcess != null)
                {
                    NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_BREAK_EVENT, (uint)dismProcess.Id);
                }
            }
        }

        private void ComboBoxDeviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxIotBuild.Items.Clear();

            LKGPlatform item = ComboBoxDeviceType.SelectedItem as LKGPlatform;
            if (item != null)
            {
                foreach (var cur in item.LkgBuilds)
                    ComboBoxIotBuild.Items.Add(cur);

                if (ComboBoxIotBuild.Items.Count > 0)
                    ComboBoxIotBuild.SelectedIndex = ComboBoxIotBuild.Items.Count - 1;
            }

            buttonFlash.IsEnabled = UpdateStartState();
        }

        private bool UpdateStartState()
        {
            if (dismProcess != null)
                return false;

            // guards for invalid data
            if (ComboBoxIotBuild.SelectedIndex < 0)
                return false;

            // guards for invalid data
            BuildInfo build = ComboBoxIotBuild.SelectedItem as BuildInfo;
            if (build == null)
                return false;

            if (RemoveableDevicesComboBox.SelectedItem == null)
                return false;

            DriveInfo driveInfo = RemoveableDevicesComboBox.SelectedItem as DriveInfo;

            if (driveInfo == null)
                return false;

            bool? isChecked = checkBoxEula.IsChecked;

            return isChecked.HasValue && isChecked.Value;
        }

        private void ComboBoxIotBuild_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }

        private void RemoveableDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }

        private void checkBoxEula_Checked(object sender, RoutedEventArgs e)
        {
            buttonFlash.IsEnabled = UpdateStartState();
        }
    }
}
