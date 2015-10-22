using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WLanTest
{
    public class Util
    {
        static readonly string PROFILE_TEMPLATE =
            "<?xml version =\"1.0\" encoding=\"US-ASCII\"?>" +
            "<WLANProfile xmlns =\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                "<name>SampleWPAPSK</name>" +
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

        public static bool Ping(string ip)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "test ping";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            try
            {
                PingReply reply = pingSender.Send(ip, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    string pingStatusText = Enum.GetName(typeof(IPStatus), reply.Status);
                    Debug.WriteLine("Ping failed - " + pingStatusText);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Ping failed with exception - " + ex.Message);
            }
            return false;
        }
    }
}
