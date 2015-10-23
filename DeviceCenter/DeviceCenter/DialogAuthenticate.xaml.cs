namespace DeviceCenter
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Security.Cryptography;

    public class UserInfo
    {
        public UserInfo()
        {
            this.DeviceName = string.Empty;
            this.UserName = string.Empty;
            this.Password = string.Empty;
            this.SavePassword = false;
        }

        public string DeviceName { get; set; }
        public string UserName { get; set; }

        /// <summary>
        /// Return plain text password
        /// </summary>
        public string Password
        {
            get
            {
                return System.Text.Encoding.UTF8.GetString(
                                    ProtectedData.Unprotect(EncryptedPassword, null, DataProtectionScope.CurrentUser));
            }


            set
            {
                EncryptedPassword = ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            }
        }
        
        public bool? SavePassword { get; set; }

        /// <summary>
        /// Password stored encrypted
        /// </summary>
        private byte[] EncryptedPassword; 
    }

    /// <summary>
    /// Interaction logic for DialogAuthenticate.xaml
    /// </summary>
    public partial class DialogAuthenticate : Window
    {
        static Dictionary<string, UserInfo> savedPasswords = new Dictionary<string, UserInfo>();
        private const string defaultUser = "Administrator";
        private const string defaultPassword = "p@ssw0rd";

        public static UserInfo GetSavedPassword(string deviceName)
        {
            UserInfo result;

            if (!savedPasswords.TryGetValue(deviceName, out result))
            {
                result = new UserInfo()
                {
                    DeviceName = deviceName,
                    UserName = defaultUser,
                    Password = defaultPassword
                };
            }
            // else read from disk

            return result;
        }

        public static void SavePassword(UserInfo userInfo)
        {
            savedPasswords.Add(userInfo.DeviceName, userInfo);
        }

        public DialogAuthenticate(UserInfo info)
        {
            InitializeComponent();

            DataContext = info;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeMethods.HideSystemMenu(this);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            UserInfo info = this.DataContext as UserInfo;

            info.UserName = editUserName.Text;
            info.Password = editPassword.Password;
            info.SavePassword = checkboxSavePassword.IsChecked;

            // save password to memory
            SavePassword(info);

            if (info.SavePassword.HasValue && info.SavePassword.Value)
            {
                // save to disk
            }

            this.DialogResult = true;

            this.Close();
        }

        private void UpdateState()
        {
            buttonOk.IsEnabled =
                editPassword.Password.Length > 0 &&
                editUserName.Text.Length > 0;
        }

        private void editPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateState();
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateState();
        }
    }
}
