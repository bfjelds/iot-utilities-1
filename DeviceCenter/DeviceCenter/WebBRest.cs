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
        private static string AppTaskUrl { get; } = "/api/taskmanager/";
        private static string PerfMgrUrl { get; } = "/api/resourcemanager/";
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

        public async Task<bool> InstallAppxAsync(string appName, IEnumerable<FileInfo> files)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "package?package=";
            url += files.First().Name;

            string boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "*/*";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));
            request.Headers.Add("Authorization", "Basic " + encodedAuth);

            using (Stream memStream = new MemoryStream())
            {
                byte[] boundaryBytesMiddle = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                byte[] boundaryBytesLast = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                await memStream.WriteAsync(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

                int count = files.Count();
                foreach (var file in files)
                {
                    string headerContentType = (file.Extension == ".cer") ? "application/x-x509-ca-cert" : "application/x-zip-compressed";
                    string header = String.Format(headerTemplate, file.Name, file.Name, headerContentType);
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    await memStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    using (FileStream fileStream = file.OpenRead())
                    {
                        await fileStream.CopyToAsync(memStream);

                        if (--count > 0)
                        {
                            await memStream.WriteAsync(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);
                        }
                        else
                        {
                            await memStream.WriteAsync(boundaryBytesLast, 0, boundaryBytesLast.Length);
                        }
                    }
                }

                request.ContentLength = memStream.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    memStream.Position = 0;
                    await memStream.CopyToAsync(requestStream);
                }
            }

            try
            {
                HttpStatusCode result = HttpStatusCode.BadRequest;

                using (HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync()))
                {
                    result = response.StatusCode;

                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            Debug.WriteLine(await sr.ReadToEndAsync());
                        }
                    }
                }

                if (result == HttpStatusCode.Accepted)
                {
                    if (await PollInstallStateAsync())
                    {
                        var installedPackages = await GetInstalledPackagesAsync();
                        foreach (AppxPackage app in installedPackages.Items)
                        {
                            if (app.Name == appName)
                            {
                                return await StartAppAsync(app.PackageRelativeId, app.PackageFullName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        public async Task<bool> PollInstallStateAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "state";
            HttpStatusCode result = HttpStatusCode.BadRequest ;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                try
                {
                    var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
                    result = response.StatusCode;
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        await Task.Delay(3000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }

            return true;
        }

        public async Task<InstalledPackages> GetInstalledPackagesAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppxApiUrl + "packages";
            try
            {
                var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return RestHelper.ProcessJsonResponse(response, typeof(InstalledPackages)) as InstalledPackages;
                }
            }
            catch (Exception wex)
            {
                // expected error
                Debug.WriteLine(wex);
            }

            return new InstalledPackages();
        }

        public async Task<bool> IsAppRunning(string appName)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + PerfMgrUrl + "processes";
            try
            {
                var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    IoTProcesses runningProcesses = RestHelper.ProcessJsonResponse(response, typeof(IoTProcesses)) as IoTProcesses;
                    foreach (IoTProcess runningProcess in runningProcesses.Items)
                    {
                        if (runningProcess.AppName == appName)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return false;
        }

        public async Task<bool> StartAppAsync(string appid, string package)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppTaskUrl
                         + "app?appid=" + RestHelper.Encode64(appid)
                         + "&package=" + RestHelper.Encode64(package);

            HttpStatusCode result = HttpStatusCode.BadRequest;
            try
            {
                result = await PostRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return (result == HttpStatusCode.OK);
        }

        public async Task<bool> StopAppAsync(string name)
        {
            string url = String.Empty;
            bool isFound = false;

            var installedPackages = await GetInstalledPackagesAsync();
            foreach (AppxPackage app in installedPackages.Items)
            {
                if (app.Name == name)
                {
                    isFound = true;
                    url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + AppTaskUrl
                         + "app?package=" + RestHelper.Encode64(app.PackageFullName);
                }
            }
            if (!isFound)
            {
                throw new ArgumentException("Application name is not valid!");
            }

            HttpStatusCode result = HttpStatusCode.BadRequest;
            try
            {
                result = await DeleteRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (result == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
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

        private async Task<HttpStatusCode> DeleteRequestAsync(string url)
        {
            Stream objStream = null;
            StreamReader objReader = null;
            Debug.WriteLine(url);
            HttpStatusCode result = HttpStatusCode.BadRequest;

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "DELETE";
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

        #region webB rest for wifi onboarding

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            int retries = 0;

            while (retries < 2)
            {
                try
                {
                    string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/interfaces";

                    var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
                    }
                }
                catch (WebException)
                {
                    retries++;
                }
            }

            return new WirelessAdapters();
        }

        public async Task<IPConfigurations> GetIPConfigurationsAsync()
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + NetworkingApiUrl + "ipconfig";

            var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
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
                var response = await RestHelper.GetOrPostRequestAsync(url, true, Username, Password);
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

        public async Task<string> ConnectToNetworkAsync(string adapterName, string ssid, string ssidPassword)
        {
            string url = HttpUrlPrfx + IpAddr.ToString() + ":" + Port + "/api/wifi/network?";
            url = url + "interface=" + adapterName.Trim("{}".ToCharArray());
            url = url + "&ssid=" + RestHelper.Encode64(ssid);
            url = url + "&op=" + "connect";
            url = url + "&createprofile=" + "yes";
            url = url + "&key=" + RestHelper.Encode64(ssidPassword);

            await RestHelper.GetOrPostRequestAsync(url, false, Username, Password);

            return string.Empty;
        }

        #endregion
    }
}
