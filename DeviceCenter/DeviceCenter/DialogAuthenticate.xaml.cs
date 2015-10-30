namespace DeviceCenter
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Security.Cryptography;
    using LoginInfoDictionary = System.Collections.Generic.Dictionary<string, UserInfo>;
    using System.Windows.Controls.Primitives;

    /// <summary>
    /// WebB login info for specified DeviceName
    /// </summary>
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
                                    ProtectedData.Unprotect(SecurePassword, null, DataProtectionScope.CurrentUser));
            }


            set
            {
                SecurePassword = ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            }
        }

        /// <summary>
        /// Returns encrypted password
        /// </summary>
        public byte[] SecurePassword { get; set; }

        public bool? SavePassword { get; set; }
    }
    
    /// <summary>
    /// Interaction logic for DialogAuthenticate.xaml
    /// </summary>
    public partial class DialogAuthenticate : Window
    {
        private const string DefaultUser = "Administrator";
        private const string DefaultPassword = "p@ssw0rd";

        // tbd - away from static?
        static LoginInfoDictionary _savedPasswords = new Dictionary<string, UserInfo>();
        static bool _firstLoaded = false;

        public static UserInfo GetSavedPassword(string deviceName)
        {
            // tbd - a hack. should be properly loaded 
            if (_firstLoaded != true)
            {
                _savedPasswords = AppData.LoadWebBUserInfo();
                _firstLoaded = true;
            }

            UserInfo result;

            if (!_savedPasswords.TryGetValue(deviceName, out result))
            {
                result = new UserInfo()
                {
                    DeviceName = deviceName,
                    UserName = DefaultUser,
                    Password = DefaultPassword
                };
            }

            return result;
        }

        public static void SavePassword(UserInfo userInfo)
        {
            if (_savedPasswords.ContainsKey(userInfo.DeviceName))
                _savedPasswords[userInfo.DeviceName] = userInfo;
            else
                _savedPasswords.Add(userInfo.DeviceName, userInfo);

            // store to permanent storage
            AppData.StoreWebBUserInfo(_savedPasswords);
        }

        public DialogAuthenticate(UserInfo info)
        {
            InitializeComponent();

            DataContext = info;

            this.editPassword.Focus();

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
            var info = this.DataContext as UserInfo;

            if (info != null)
            {
                info.UserName = editUserName.Text;
                info.Password = editPassword.Password;
                info.SavePassword = checkboxSavePassword.IsChecked;

                if (info.SavePassword.HasValue && info.SavePassword.Value)
                {
                    SavePassword(info);
                }
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

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter && buttonOk.IsEnabled)
            {
                buttonOk.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
    }
}
