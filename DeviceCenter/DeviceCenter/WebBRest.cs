using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using DeviceCenter.DataContract;
using DeviceCenter.Helper;

namespace DeviceCenter
{
    public class WebBRest
    {
        public static IPAddress IpAddr;

        public string Username { get; set; }
        public string Password { get; set; }
        public static string Port { get; set; } = "8080";
        public static string Admin { get; } = "Administrator";
        public static string AdminPwd { get; set; } = "p@ssw0rd";
        private static string DeviceApiUrl { get; } = "/api/iot/device/";
        private static string ControlApiUrl { get; } = "/api/control/";
        private static string NetworkingApiUrl { get; } = "/api/networking/";
        private static string AppxApiUrl { get; } = "/api/appx/packagemanager/";
        private static string HttpUrlPrfx { get; } = "http://";

        public WebBRest(IPAddress ip, string username, string password)
        {
            IpAddr = ip;
            Username = username;
            Password = password;
        }

        public async Task<bool> SetDeviceNameAsync(string newDeviceName)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + DeviceApiUrl + "name?newdevicename=";
            url += Encode64(newDeviceName);

            try
            {
                await PostRequest(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> SetPasswordAsync(string oldPassword, string newPassword)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + DeviceApiUrl + "password?";
            url = url + "oldpassword=" + Encode64(oldPassword);
            url = url + "&newpassword=" + Encode64(newPassword);

            try
            {
                await PostRequest(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            if (Username == Admin)
            {
                AdminPwd = newPassword;
            }
            Password = newPassword;

            return true;
        }

        public async Task<bool> Restart()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + ControlApiUrl + "restart";

            try
            {
                await PostRequest(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        //public async Task<bool> InstallAppx(File appxFile, File certFile)
        //{

        //}

        private async Task<HttpStatusCode> PostRequest(string url)
        {
            Stream objStream = null;
            StreamReader objReader = null;
            Debug.WriteLine(url);
            HttpStatusCode result = HttpStatusCode.BadRequest;

            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Credentials = new NetworkCredential(Username, Password);
                req.ContentLength = 0;

                HttpWebResponse response = (HttpWebResponse)(await req.GetResponseAsync());
                result = response.StatusCode;
                if (result == HttpStatusCode.OK)
                {
                    objStream = response.GetResponseStream();
                    objReader = new StreamReader(objStream);
                    string respData = objReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result;
        }

        public static string Encode64(string toEncodeString)
        {
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncodeString.Trim());
            string string64 = System.Convert.ToBase64String(toEncodeAsBytes);

            // Ref: http://www.werockyourweb.com/url-escape-characters/
            string64 = string64.Replace(" ", "20%");
            string64 = string64.Replace("$", "24%");
            string64 = string64.Replace("&", "26%");
            string64 = string64.Replace("`", "60%");
            string64 = string64.Replace(":", "%3A");
            string64 = string64.Replace("<", "%3C");
            string64 = string64.Replace(">", "%3E");
            string64 = string64.Replace("[", "%5B");
            string64 = string64.Replace("]", "%5D");
            string64 = string64.Replace("{", "%7B");
            string64 = string64.Replace("}", "%7D");
            string64 = string64.Replace("\"", "22%");
            string64 = string64.Replace("+", "%2B");
            string64 = string64.Replace("#", "23%");
            string64 = string64.Replace("%", "25%");
            string64 = string64.Replace("@", "40%");
            string64 = string64.Replace("/", "%2F");
            string64 = string64.Replace(";", "%3B");
            string64 = string64.Replace("=", "%3D");
            string64 = string64.Replace("?", "%3F");
            string64 = string64.Replace("\\", "%5C");
            string64 = string64.Replace("^", "%5E");
            string64 = string64.Replace("|", "%7C");
            string64 = string64.Replace("~", "%7E");
            string64 = string64.Replace("'", "27%");
            string64 = string64.Replace(",", "%2C");

            return string64;
        }

        #region webB rest for wifi onboarding

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + NetworkingApiUrl + "ipconfig";

            var response = await RestHelper.MakeRequest(url, Username, Password);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
            }

            return new WirelessAdapters();
        }

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(string adapterName)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/networks?";
            url += "interface=" + adapterName.Trim("{}".ToCharArray());

            try
            {
                var response = await RestHelper.MakeRequest(url, Username, Password);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return RestHelper.ProcessResponse(response, typeof(AvailableNetworks)) as AvailableNetworks;
                }
            }
            catch(Exception wex)
            {
                // expected error
                Debug.WriteLine(wex);
            }

            return new AvailableNetworks();
        }

        public async Task<string> ConnectToNetworkAsync(string adapterName, string ssid, string password)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/network?";
            url = url + "interface=" + adapterName.Trim("{}".ToCharArray());
            url = url + "&ssid=" + Encode64(ssid);
            url = url + "&op=" + "connect";
            url = url + "&createprofile=" + "yes";

            await RestHelper.MakeRequest(url, Username, Password);

            return string.Empty;
        }

        #endregion
    }
}
