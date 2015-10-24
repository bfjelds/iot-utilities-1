using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.WlanAPIs
{
    public class Util
    {
        public const string WLAN_PROFILE_NAME = "AthensSoftAP";

        static readonly string PROFILE_TEMPLATE =
            "<?xml version =\"1.0\" encoding=\"US-ASCII\"?>" +
            "<WLANProfile xmlns =\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                string.Format("<name>{0}</name>", WLAN_PROFILE_NAME) +
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

        static readonly string SECURITY_SECTION_TEMPLATE = 
            "<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>$key</keyMaterial></sharedKey>";

        static readonly string[] AuthAlgStrings = new string[] {
            "",
            "open",
            "shared",
            "WPA",
            "WPAPSK",
            "",
            "WPA2",
            "WPA2PSK"
        };

        public static string MakeProfileString(string ssid, uint authAlg, uint cipherAlg, string password)
        {
            string profileStr = PROFILE_TEMPLATE;
            profileStr = profileStr.Replace("$ssid", ssid);
            profileStr = profileStr.Replace("$authentication", AuthAlgToString(authAlg));
            profileStr = profileStr.Replace("$encryption", CipherAlgToString(cipherAlg));

            string securityStr = SECURITY_SECTION_TEMPLATE;
            securityStr = securityStr.Replace("$key", "password");
            profileStr = profileStr.Replace("$securitySection", securityStr);

            return profileStr;
        }

        static string AuthAlgToString(uint alg)
        {
            if(alg != 0 && alg < AuthAlgStrings.Length)
            {
                return AuthAlgStrings[alg];
            }

            Debug.Fail("debug break");
            return string.Empty;
        }

        static string CipherAlgToString(uint alg)
        {
            switch (alg)
            {
                case 0:
                    return "none";
                case 257:
                case 5:
                case 1:
                    return "WEP";
                case 2:
                    return "TKIP";
                case 4:
                    return "AES";
                default:
                    return "undefined";
            }
        }

        public static string GetStringForSSID(WlanInterop.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        public static bool IsDHCPIPAddress(string ipStr)
        {
            if(string.IsNullOrWhiteSpace(ipStr))
            {
                return false;
            }

            return ipStr.StartsWith("192.168");
        }

        public static async Task<bool> Ping(string ip)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "test ping";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            try
            {
                PingReply reply = await pingSender.SendPingAsync(ip, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    string pingStatusText = Enum.GetName(typeof(IPStatus), reply.Status);
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
            if(errorCode != 0)
            {
                var ex = new WLanException(errorCode, method);
                Error(ex.ToString());
                // throw ex;
            }
        }

        public static void Info(string message, params object[] paras)
        {
            var msg = string.Format(message, paras);
            Console.WriteLine(msg);
            Debug.WriteLine("Info: " + msg);
        }

        public static void Error(string message, params object[] paras)
        {
            var msg = string.Format(message, paras);
            Console.WriteLine(msg);
            Debug.WriteLine("Error: " + msg);
        }

        public static void RunNetshElevated(string arguments)
        {
            var procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = true;
            procInfo.WorkingDirectory = @"C:\Windows\System32";
            procInfo.FileName = @"C:\Windows\System32\netsh.exe";
            procInfo.Arguments = arguments;
            procInfo.Verb = "runas";
            procInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Info("RunNetshElevated [{0}]", arguments);
            try
            {
                Process proc = new Process();
                proc.StartInfo = procInfo;
                proc.Start();

                Console.WriteLine("Successfully elevated!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to elevate.");
            }
        }

        public static string GetNameByGuid(Guid guid)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in interfaces)
            {
                if(Guid.Parse(adapter.Id) == guid)
                {
                    Info("Find name [{0}] for guid [{1}]", adapter.Name, guid.ToString());
                    return adapter.Name;
                }
            }

            Error("Can't Find name for guid [{1}]", guid.ToString());
            return string.Empty;
        }
    }
}
