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
        private const string AppTaskUrl = "/api/taskmanager/";
        private const string PerfMgrUrl = "/api/resourcemanager/";

        private readonly RestHelper _restHelper;

        public WebBRest(IPAddress ipAddress, UserInfo userInfo)
        {
            this._restHelper = new RestHelper(ipAddress, userInfo);
        }

        public async Task<bool> SetDeviceNameAsync(string newDeviceName)
        {
            string url = DeviceApiUrl + "name?newdevicename=";
            url += RestHelper.Encode64(newDeviceName);

            try
            {
                await _restHelper.PostRequestAsync(url);
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
            var url = DeviceApiUrl + "password?";
            url = url + "oldpassword=" + RestHelper.Encode64(oldPassword);
            url = url + "&newpassword=" + RestHelper.Encode64(newPassword);

            try
            {
                await _restHelper.PostRequestAsync(url);

                // resaves the password
                _restHelper.DeviceAuthentication.Password = newPassword;
                DialogAuthenticate.SavePassword(_restHelper.DeviceAuthentication);
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
            var url = ControlApiUrl + "restart";

            try
            {
                await _restHelper.PostRequestAsync(url);
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
            var url = AppxApiUrl + "package?package=";
            url += files.First().Name;

            string boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x");

            var request = (HttpWebRequest)WebRequest.Create(this._restHelper.CreateUri(url));
            request.Accept = "*/*";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            var encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this._restHelper.DeviceAuthentication.UserName + ":" + this._restHelper.DeviceAuthentication.Password));
            request.Headers.Add("Authorization", "Basic " + encodedAuth);

            using (Stream memStream = new MemoryStream())
            {
                var boundaryBytesMiddle = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                var boundaryBytesLast = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                await memStream.WriteAsync(boundaryBytesMiddle, 0, boundaryBytesMiddle.Length);

                var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

                var count = files.Count();

                foreach (var file in files)
                {
                    var headerContentType = (file.Extension == ".cer") ? "application/x-x509-ca-cert" : "application/x-zip-compressed";
                    var header = String.Format(headerTemplate, file.Name, file.Name, headerContentType);
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    await memStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    using (var fileStream = file.OpenRead())
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
                var result = HttpStatusCode.BadRequest;

                using (HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync()))
                {
                    result = response.StatusCode;

                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (var sr = new StreamReader(stream))
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
                        foreach (var app in installedPackages.Items)
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
            const string URL = AppxApiUrl + "state";
            var result = HttpStatusCode.BadRequest;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                try
                {
                    var response = await _restHelper.GetOrPostRequestAsync(URL, true);
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
            var url = AppxApiUrl + "packages";
            try
            {
                var response = await this._restHelper.GetOrPostRequestAsync(url, true);
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
                var response = await this._restHelper.GetOrPostRequestAsync(url, true);
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
            string url = AppTaskUrl + "app?appid=" + RestHelper.Encode64(appid) 
                + "&package=" + RestHelper.Encode64(package);

            HttpStatusCode result = HttpStatusCode.BadRequest;
            try
            {
                result = await this._restHelper.PostRequestAsync(url);
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
            foreach (var app in installedPackages.Items)
            {
                if (app.Name != appName) continue;
                isFound = true;
                url = AppTaskUrl + "app?package=" + RestHelper.Encode64(app.PackageFullName);
            }
            if (!isFound)
            {
                throw new ArgumentException("Application name is not valid!");
            }

            var result = HttpStatusCode.BadRequest;
            try
            {
                result = await this._restHelper.DeleteRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result == HttpStatusCode.OK;
        }

        #region webB rest for wifi onboarding

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            var url = "/api/wifi/interfaces";

            var response = await this._restHelper.GetOrPostRequestAsync(url, true);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
            }

            return new WirelessAdapters();
        }

        public async Task<IPConfigurations> GetIpConfigurationsAsync()
        {
            const string URL = NetworkingApiUrl + "ipconfig";

            var response = await this._restHelper.GetOrPostRequestAsync(URL, true);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return RestHelper.ProcessJsonResponse(response, typeof(IPConfigurations)) as IPConfigurations;
            }

            return new IPConfigurations();
        }

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(string adapterName)
        {
            var url = "/api/wifi/networks?interface=" + adapterName.Trim("{}".ToCharArray());

            try
            {
                var response = await this._restHelper.GetOrPostRequestAsync(url, true);
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
            if (!string.IsNullOrEmpty(ssidPassword))
            {
                url = url + "&key=" + RestHelper.Encode64(ssidPassword);
            }

            await this._restHelper.GetOrPostRequestAsync(url, false);

            return string.Empty;
        }

        #endregion
    }
}
