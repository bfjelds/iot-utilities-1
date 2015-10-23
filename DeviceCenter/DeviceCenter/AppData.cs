// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using Microsoft.Win32;
using LoginInfoDictionary = System.Collections.Generic.Dictionary<string, DeviceCenter.UserInfo>;

namespace DeviceCenter
{
    /// <summary>
    /// Store app state across app restart and versions.
    /// </summary>
    public class AppData
    {
        private const string HKCU = "HKEY_CURRENT_USER";
        private const string WebBLoginRootKey = "Software\\IoT\\DeviceCenter\\WebBUserInfo";
        private const string WebBUserName = "UserName";
        private const string WebBPassword = "Password";

        /// <summary>
        /// Store per device login info to registry. Key is device name.
        /// </summary>
        /// <param name="userInfoList"></param>
        public static void StoreWebBUserInfo(LoginInfoDictionary userInfoList)
        {
            foreach (var x in userInfoList)
            {   
                var deviceNameKey = HKCU + "\\" + WebBLoginRootKey + "\\" + x.Key;
                Registry.SetValue(deviceNameKey, WebBUserName, x.Value.UserName);
                Registry.SetValue(deviceNameKey, WebBPassword, x.Value.SecurePassword);
            }
        }

        /// <summary>
        /// Load per device login info from registry
        /// </summary>
        /// <returns></returns>
        public static LoginInfoDictionary LoadWebBUserInfo()
        {
            var userInfoList = new LoginInfoDictionary();

            var key = Registry.CurrentUser.OpenSubKey(WebBLoginRootKey);

            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var deviceNameKey = HKCU + "\\" + WebBLoginRootKey + "\\" + subKeyName;
                    var userName = Registry.GetValue(deviceNameKey, WebBUserName, null);
                    var password = Registry.GetValue(deviceNameKey, WebBPassword, null);

                    var userInfo = new UserInfo()
                    {
                        UserName = userName as string,
                        SecurePassword = password as byte[],
                        SavePassword = true,
                        DeviceName = subKeyName
                    };

                    userInfoList.Add(subKeyName, userInfo);
                }

                key.Close();
            }

            return userInfoList;           
        }
    }
}
