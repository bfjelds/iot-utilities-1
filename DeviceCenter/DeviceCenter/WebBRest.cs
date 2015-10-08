using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



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
            byte[] newDeviceNameBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(newDeviceName.Trim());
            url += Encode64(newDeviceNameBytes);

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
            url += "oldpassword=";
            byte[] oldPasswordBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(oldPassword.Trim());
            byte[] newPasswordBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(newPassword.Trim());
            url += Encode64(oldPasswordBytes);
            url = url + "&newpassword=" + Encode64(newPasswordBytes);

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

        //public async void InstallAppx()
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

        public static string Encode64(byte[] toEncodeAsBytes)
        {
            string name64 = System.Convert.ToBase64String(toEncodeAsBytes);
            name64 = name64.Replace(" ", "20%");
            name64 = name64.Replace("$", "24%");
            name64 = name64.Replace("&", "26%");
            name64 = name64.Replace("`", "60%");
            name64 = name64.Replace(":", "%3A");
            name64 = name64.Replace("<", "%3C");
            name64 = name64.Replace(">", "%3E");
            name64 = name64.Replace("[", "%5B");
            name64 = name64.Replace("]", "%5D");
            name64 = name64.Replace("{", "%7B");
            name64 = name64.Replace("}", "%7D");
            name64 = name64.Replace("\"", "22%");
            name64 = name64.Replace("+", "%2B");
            name64 = name64.Replace("#", "23%");
            name64 = name64.Replace("%", "25%");
            name64 = name64.Replace("@", "40%");
            name64 = name64.Replace("/", "%2F");
            name64 = name64.Replace(";", "%3B");
            name64 = name64.Replace("=", "%3D");
            name64 = name64.Replace("?", "%3F");
            name64 = name64.Replace("\\", "%5C");
            name64 = name64.Replace("^", "%5E");
            name64 = name64.Replace("|", "%7C");
            name64 = name64.Replace("~", "%7E");
            name64 = name64.Replace("'", "27%");
            name64 = name64.Replace(",", "%2C");
            return name64;
        }

    }
}
