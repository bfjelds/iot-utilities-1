using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

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
                await PostRequestAsync(url);
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
                await PostRequestAsync(url);
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

        public async Task<bool> RestartAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + ControlApiUrl + "restart";

            try
            {
                await PostRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        //public async Task<bool> InstallAppxAsync(string appxFilePath,
        //                                    string certFilePath)
        //string dependFilePath = null)
        //{
        //    string appxFileContent, certFileContent;
        //    appxFileContent = File.ReadAllText(appxFilePath);
        //    certFileContent = File.ReadAllText(certFilePath);
        //    if (!String.IsNullOrEmpty(dependFilePath))
        //    {
        //        dependFileContent = File.ReadAllText(dependFilePath);
        //    }

        //    AppxPackage appx = new AppxPackage(appxFileContent, certFileContent);
        //    MemoryStream stream1 = new MemoryStream();
        //    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppxPackage));
        //    ser.WriteObject(stream1, appx);

        //    string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "package?package=";
        //    url += Path.GetFileName(appxFilePath);

        //    try
        //    {
        //        await PostRequestAsync(url, ser.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //        return false;
        //    }

        //    return true;
        //}

        public async Task<bool> InstallAppxAsync()
        {
            //HttpClientHandler handler = new HttpClientHandler();
            //handler.Credentials = new NetworkCredential(Username, Password);
            //var client = new HttpClient();

            using (var handler = new HttpClientHandler { Credentials = new NetworkCredential(Username, Password) })
            using (var client = new HttpClient(handler))
            {
                string appxPath = @"C:\Users\tenglu\Documents\KinectDepthUWP\x86\";
                string certPath = @"C:\Users\tenglu\Documents\KinectDepthUWP\x86\";
                appxPath += @"BasicDepthUAP.appx";
                certPath += @"testroot-sha2.cer";

                string formDataBoundary = String.Format("----------------------{0:N}", Guid.NewGuid());
                var content = new MultipartFormDataContent(formDataBoundary);
                content.Add(new StreamContent(File.Open(appxPath, FileMode.Open)), "appxFile", "BasicDepthUAP.appx");
                //content.Add(new StreamContent(File.Open(certPath, FileMode.Open)), "certFile", "testroot-sha2.cer");

                string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "package?package=";
                url += Path.GetFileName(appxPath);

                try
                {
                    var result = await client.PostAsync(url, content);
                    if (result.StatusCode == HttpStatusCode.Accepted)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        private async Task<HttpStatusCode> PostRequestAsync(string url)
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

        private async Task<HttpStatusCode> PostRequestAsync(string url, string jsonPayload)
        {
            Stream objStream = null;
            StreamReader objReader = null;
            Debug.WriteLine(url);
            HttpStatusCode result = HttpStatusCode.BadRequest;

            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/json; charset=utf-8";
                req.Credentials = new NetworkCredential(Username, Password);

                using (var streamWriter = new StreamWriter(req.GetRequestStream()))
                {
                    streamWriter.Write(jsonPayload);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

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

    }
}
