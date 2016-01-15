﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Security.Cryptography;

namespace IotCoreAppDeploymentCs
{
    /// <summary>
    /// WebB login info for specified DeviceName
    /// </summary>
    public class UserInfo
    {
        public UserInfo()
        {
            this.UserName = string.Empty;
            this.Password = string.Empty;
            this.SavePassword = false;
        }

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
}