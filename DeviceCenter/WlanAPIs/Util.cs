// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WlanAPIs
{
    public class Util
    {
        public const string WlanProfileName = "AthensSoftAP";
        public const string AthensSoftApAuthentication = "WPA2PSK";
        public const string AthensSoftApEncryption = "AES";
        static readonly string ProfileTemplate =
            "<?xml version =\"1.0\" encoding=\"US-ASCII\"?>" +
            "<WLANProfile xmlns =\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                $"<name>{WlanProfileName}</name>" +
                "<SSIDConfig>" +
                    "<SSID>" +
                        "<name>$ssid</name>" +
                    "</SSID>" +
                "</SSIDConfig>" +
                "<connectionType>ESS</connectionType>" +
                "<connectionMode>auto</connectionMode>" +
                "<autoSwitch>false</autoSwitch>" +
                "<MSM>" +
                    "<security>" +
                        "<authEncryption>" +
                            "<authentication>$authentication</authentication>" +
                            "<encryption>$encryption</encryption>" +
                            "<useOneX>false</useOneX>" +
                        "</authEncryption>$securitySection" +
                    "</security>" +
                "</MSM>" +
            "</WLANProfile>";

        static readonly string SecuritySectionTemplate =
            "<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>$key</keyMaterial></sharedKey>";

        public static string MakeProfileString(string ssid, uint authAlg, uint cipherAlg, string password)
        {
            var profileStr = ProfileTemplate;
            profileStr = profileStr.Replace("$ssid", ssid);
            profileStr = profileStr.Replace("$authentication", AthensSoftApAuthentication);
            profileStr = profileStr.Replace("$encryption", AthensSoftApEncryption);

            var securityStr = SecuritySectionTemplate;
            securityStr = securityStr.Replace("$key", "password");
            profileStr = profileStr.Replace("$securitySection", securityStr);

            return profileStr;
        }

        public static string GetStringForSsid(WlanInterop.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.Ssid, 0, (int)ssid.SsidLength);
        }

        /// <summary>
        /// tbd comment
        /// </summary>
        /// <param name="ipStr"></param>
        /// <returns></returns>
        public static bool IsDhcpipAddress(string ipStr)
        {
            return !string.IsNullOrWhiteSpace(ipStr) && ipStr.StartsWith("192.168.173");
        }

        /// <summary>
        /// tbd comment
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static async Task<bool> Ping(string ip)
        {
            var pingSender = new Ping();
            var options = new PingOptions();

            // Create a buffer of 32 bytes of data to be transmitted.
            const string DATA = "test ping";
            var buffer = Encoding.ASCII.GetBytes(DATA);
            const int TIMEOUT = 120;
            try
            {
                var reply = await pingSender.SendPingAsync(ip, TIMEOUT, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    var pingStatusText = Enum.GetName(typeof(IPStatus), reply.Status);
                    Info("Ping failed - " + pingStatusText);
                }
            }
            catch (Exception ex)
            {
                Info("Ping failed with exception - " + ex.Message);
            }

            return false;
        }

        [DebuggerStepThrough]
        public static void ThrowIfFail(uint errorCode, string method)
        {
            if (errorCode == 0) return;
            var ex = new WLanException(errorCode, method);
            Error(ex.ToString());
            throw ex;
        }

        public static void Info(string message, params object[] paras)
        {
            var msg = string.Format(message, paras);
            // Console.WriteLine(msg);
            Debug.WriteLine("Info: " + msg);
        }

        public static void Error(string message, params object[] paras)
        {
            var msg = string.Format(message, paras);
            // Console.WriteLine(msg);
            Debug.WriteLine("Error: " + msg);
        }

        public static bool RunNetshElevated(string arguments)
        {
            var procInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = System.Environment.SystemDirectory,
                FileName = Path.Combine(System.Environment.SystemDirectory, @"netsh.exe"),
                Arguments = arguments,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Info("RunNetshElevated [{0}]", arguments);
            try
            {
                var proc = new Process { StartInfo = procInfo };
                proc.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to run elevated.");
                return false;
            }

            return true;
        }
    }
}
