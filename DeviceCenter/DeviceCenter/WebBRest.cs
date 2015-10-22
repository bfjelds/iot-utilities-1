using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            url += RestHelper.Encode64(newDeviceName);

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
            url = url + "oldpassword=" + RestHelper.Encode64(oldPassword);
            url = url + "&newpassword=" + RestHelper.Encode64(newPassword);

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

        public async Task<bool> InstallAppxAsync(string[] fileNames)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "package?package=";
            url += fileNames[0];

            string boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "*/*";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Referer = @"http://10.125.140.92:8080/AppManager.htm";
            request.Method = "POST";
            request.KeepAlive = true;
            string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));
            request.Headers.Add("Authorization", "Basic " + encodedAuth);

            Stream memStream = new MemoryStream();

            byte[] boundaryBytesMiddle = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] boundaryBytesLast = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            memStream.Write(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

            // TODO: Determine the way to load the file path
            string path = @"C:\Users\tenglu\Documents\DeployTestAppx\InternetRadio\";

            for (int i = 0; i < fileNames.Length; i++)
            {
                string headerContentType = (fileNames[i].Substring(fileNames[i].Length - 4) == ".cer") ? "application/x-x509-ca-cert" : "application/x-zip-compressed";
                string header = String.Format(headerTemplate, fileNames[i], fileNames[i], headerContentType);
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                memStream.Write(headerBytes, 0, headerBytes.Length);

                FileStream fileStream = new FileStream(path + fileNames[i], FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }

                if (i < fileNames.Length - 1)
                {
                    memStream.Write(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);
                }
                else
                {
                    memStream.Write(boundaryBytesLast, 0, boundaryBytesLast.Length);
                }

                fileStream.Close();
            }

            request.ContentLength = memStream.Length;

            Stream requestStream = request.GetRequestStream();

            memStream.Position = 0;
            byte[] tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                HttpStatusCode result = HttpStatusCode.BadRequest;
                result = response.StatusCode;
                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string respData = sr.ReadToEnd();

                response.Close();
                request = null;
                response = null;

                if (result == HttpStatusCode.Accepted)
                {
                    Thread.Sleep(20000);
                    bool x = await PollInstallState();
                    if (x)
                    {
                        // TODO: start app
                    }
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
            }

            return true;
        }

        public async Task<bool> PollInstallState()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "state";
            try
            {
                var response = await RestHelper.MakeRequest(url, true, Username, Password);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return false;
                }
                else
                {
                    throw new WebException("Bad response when getting deployment status.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
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
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));
                req.Headers.Add("Authorization", "Basic " + encodedAuth);
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

        //private async Task<HttpStatusCode> PostRequestAsync(string url, string jsonPayload)
        //{
        //    Stream objStream = null;
        //    StreamReader objReader = null;
        //    Debug.WriteLine(url);
        //    HttpStatusCode result = HttpStatusCode.BadRequest;

        //    try
        //    {
        //        HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
        //        req.Method = "POST";
        //        req.ContentType = "application/json; charset=utf-8";
        //        req.Credentials = new NetworkCredential(Username, Password);

        //        using (var streamWriter = new StreamWriter(req.GetRequestStream()))
        //        {
        //            streamWriter.Write(jsonPayload);
        //            streamWriter.Flush();
        //            streamWriter.Close();
        //        }

        //        HttpWebResponse response = (HttpWebResponse)(await req.GetResponseAsync());
        //        result = response.StatusCode;
        //        if (result == HttpStatusCode.OK)
        //        {
        //            objStream = response.GetResponseStream();
        //            objReader = new StreamReader(objStream);
        //            string respData = objReader.ReadToEnd();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }
        //    return result;
        //}

        #region webB rest for wifi onboarding

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/interfaces";

            var response = await RestHelper.MakeRequest(url, true, Username, Password);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
            }

            return new WirelessAdapters();
        }

        public async Task<IPConfigurations> GetIPConfigurationsAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + NetworkingApiUrl + "ipconfig";

            var response = await RestHelper.MakeRequest(url, true, Username, Password);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(IPConfigurations)) as IPConfigurations;
            }

            return new IPConfigurations();
        }

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(string adapterName)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/networks?";
            url += "interface=" + adapterName.Trim("{}".ToCharArray());

            try
            {
                var response = await RestHelper.MakeRequest(url, true, Username, Password);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return RestHelper.ProcessJsonResponse(response, typeof(AvailableNetworks)) as AvailableNetworks;
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
            url = url + "&ssid=" + RestHelper.Encode64(ssid);
            url = url + "&op=" + "connect";
            url = url + "&createprofile=" + "yes";

            await RestHelper.MakeRequest(url, false, Username, Password);

            return string.Empty;
        }

        #endregion
    }
}
