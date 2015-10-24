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
        private const string DeviceApiUrl = "/api/iot/device/";
        private const string ControlApiUrl = "/api/control/";
        private const string NetworkingApiUrl = "/api/networking/";
        private const string AppxApiUrl = "/api/appx/packagemanager/";
        private const string AppTaskUrl = "/api/taskmanager/app";
        private const string PerfMgrUrl = "/api/resourcemanager/";

        private RestHelper restHelper;

        public WebBRest(IPAddress ipAddress, UserInfo userInfo)
        {
            this.restHelper = new RestHelper(ipAddress, userInfo);
        }

        public async Task<bool> SetDeviceNameAsync(string newDeviceName)
        {
            string url = DeviceApiUrl + "name?newdevicename=";
            url += RestHelper.Encode64(newDeviceName);

            try
            {
                await restHelper.PostRequestAsync(url);
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
            string url = DeviceApiUrl + "password?";
            url = url + "oldpassword=" + RestHelper.Encode64(oldPassword);
            url = url + "&newpassword=" + RestHelper.Encode64(newPassword);

            try
            {
                await restHelper.PostRequestAsync(url);

                // resaves the password
                restHelper.DeviceAuthentication.Password = newPassword;
                DialogAuthenticate.SavePassword(restHelper.DeviceAuthentication);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task<bool> RestartAsync()
        {
            string url = ControlApiUrl + "restart";

            try
            {
                await restHelper.PostRequestAsync(url);
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
            string url = AppxApiUrl + "package?package=";
            url += files.First().Name;

            string boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.restHelper.CreateUri(url));
            request.Accept = "*/*";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.restHelper.DeviceAuthentication.UserName + ":" + this.restHelper.DeviceAuthentication.Password));
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
            string url = AppxApiUrl + "state";
            HttpStatusCode result = HttpStatusCode.BadRequest;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                try
                {
                    var response = await restHelper.GetOrPostRequestAsync(url, true);
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
            string url = AppxApiUrl + "packages";
            try
            {
                var response = await this.restHelper.GetOrPostRequestAsync(url, true);
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
            string url = PerfMgrUrl + "processes";
            try
            {
                var response = await this.restHelper.GetOrPostRequestAsync(url, true);
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
            string url = AppTaskUrl + "?appid=" + RestHelper.Encode64(appid) 
                + "&package=" + RestHelper.Encode64(package);

            HttpStatusCode result = HttpStatusCode.BadRequest;
            try
            {
                result = await this.restHelper.PostRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            return (result == HttpStatusCode.OK);
        }

        public async Task<bool> StopAppAsync(string appName)
        {
            string url = String.Empty;
            bool isFound = false;

            var installedPackages = await GetInstalledPackagesAsync();
            foreach (AppxPackage app in installedPackages.Items)
            {
                if (app.Name == appName)
                {
                    isFound = true;
                    url = AppTaskUrl + "app?package=" + RestHelper.Encode64(app.PackageFullName);
                }
            }
            if (!isFound)
            {
                throw new ArgumentException("Application name is not valid!");
            }

            HttpStatusCode result = HttpStatusCode.BadRequest;
            try
            {
                result = await this.restHelper.DeleteRequestAsync(url);
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

        #region webB rest for wifi onboarding

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            string url = "/api/wifi/interfaces";

            var response = await this.restHelper.GetOrPostRequestAsync(url, true);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
            }

            return new WirelessAdapters();
        }

        public async Task<IPConfigurations> GetIPConfigurationsAsync()
        {
            string url = NetworkingApiUrl + "ipconfig";

            var response = await this.restHelper.GetOrPostRequestAsync(url, true);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(IPConfigurations)) as IPConfigurations;
            }

            return new IPConfigurations();
        }

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(string adapterName)
        {
            string url = "/api/wifi/networks?interface=" + adapterName.Trim("{}".ToCharArray());

            try
            {
                var response = await this.restHelper.GetOrPostRequestAsync(url, true);
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
            string url = "/api/wifi/network?";
            url = url + "interface=" + adapterName.Trim("{}".ToCharArray());
            url = url + "&ssid=" + RestHelper.Encode64(ssid);
            url = url + "&op=" + "connect";
            url = url + "&createprofile=" + "yes";
            url = url + "&key=" + RestHelper.Encode64(ssidPassword);

            await this.restHelper.GetOrPostRequestAsync(url, false);

            return string.Empty;
        }

        #endregion
    }
}
