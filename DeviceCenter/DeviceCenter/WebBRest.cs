﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DeviceCenter.DataContract;
using DeviceCenter.Helper;

namespace DeviceCenter
{
    public class WebBRest
    {
        private const string DeviceApiUrl = "/api/iot/device/";
        private const string OsInfo = "/api/os/info";
        private const string ControlApiUrl = "/api/control/";
        private const string NetworkingApiUrl = "/api/networking/";
        private const string AppxApiUrl = "/api/appx/packagemanager/";
        private const string AppTaskUrl = "/api/taskmanager/";
        private const string PerfMgrUrl = "/api/resourcemanager/";

        private const int QueryInterval = 3000;

        private readonly RestHelper _restHelper;

        public class RestError : Exception
        {
            public RestError(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        public WebBRest(Window parent, IPAddress ipAddress, UserInfo userInfo)
        {
            this._restHelper = new RestHelper(parent, ipAddress, userInfo);
        }

        public Task<OsInfo> GetDeviceInfoAsync()
        {
            return Task<OsInfo>.Run(async () => 
            {
                string url = OsInfo;

                try
                {
                    using (var response = await _restHelper.GetOrPostRequestAsync(url, true))
                    {
                        var data = RestHelper.ProcessJsonResponse(response, typeof(OsInfo)) as OsInfo;

                        // data might be null, caller should check
                        return data;
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    return null;
                }
            });
        }

        public async Task<bool> SetDeviceNameAsync(string newDeviceName)
        {
            string url = DeviceApiUrl + "name?newdevicename=";
            url += RestHelper.EscapeUriString(newDeviceName);

            try
            {
                var result = await _restHelper.PostRequestAsync(url, string.Empty);

                if (result == HttpStatusCode.OK)
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

        public async Task<bool> SetPasswordAsync(string oldPassword, string newPassword)
        {
            var url = DeviceApiUrl + "password?";
            url = url + "oldpassword=" + RestHelper.EscapeUriString(oldPassword);
            url = url + "&newpassword=" + RestHelper.EscapeUriString(newPassword);

            try
            {
                HttpStatusCode result = await _restHelper.PostRequestAsync(url, oldPassword);

                if (result == HttpStatusCode.OK)
                {
                    // resaves the password
                    _restHelper.DeviceAuthentication.Password = newPassword;
                    DialogAuthenticate.SavePassword(_restHelper.DeviceAuthentication);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show(
                    Strings.Strings.MessageBadPassword,
                    Strings.Strings.AppNameDisplay, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Exclamation);
                return false;
            }

            return false;
        }

        public async Task<bool> RestartAsync()
        {
            var url = ControlApiUrl + "restart";

            try
            {
                var result = await _restHelper.PostRequestAsync(url, string.Empty);

                if (result == HttpStatusCode.OK)
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
                        return await StartAppAsync(appName);
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

        public async Task<bool> PollInstallStateAsync()
        {
            const string URL = AppxApiUrl + "state";
            var result = HttpStatusCode.BadRequest;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                try
                {
                    using (var response = await _restHelper.GetOrPostRequestAsync(URL, true))
                    {
                        result = response.StatusCode;
                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            await Task.Delay(QueryInterval);
                        }
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
                using (var response = await this._restHelper.GetOrPostRequestAsync(url, true))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return RestHelper.ProcessJsonResponse(response, typeof(InstalledPackages)) as InstalledPackages;
                    }
                }
            }
            catch (Exception wex)
            {
                // expected error
                Debug.WriteLine(wex);
            }

            return null;
        }

        public async Task<bool> IsAppRunning(string appName)
        {
            const string URL = PerfMgrUrl + "processes";
            try
            {
                using (var response = await this._restHelper.GetOrPostRequestAsync(URL, true))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException();
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var runningProcesses = RestHelper.ProcessJsonResponse(response, typeof(IoTProcesses)) as IoTProcesses;
                        if (runningProcesses != null && runningProcesses.Items.Any(runningProcess => runningProcess.AppName == appName))
                        {
                            return true;
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

        public async Task<bool> StartAppAsync(string appName)
        {
            var result = HttpStatusCode.BadRequest;
            try
            {
                var installedPackages = await GetInstalledPackagesAsync();

                if (installedPackages == null)
                {
                    // REST API error when getting installed packages
                    return false;
                }

                foreach (var app in installedPackages.Items)
                {
                    if (app.Name == appName)
                    {
                        var url = AppTaskUrl + "app?appid=" + RestHelper.EscapeUriString(app.PackageRelativeId)
                                     + "&package=" + RestHelper.EscapeUriString(app.PackageFullName);

                        result = await this._restHelper.PostRequestAsync(url, string.Empty);
                    }
                }
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
            var url = String.Empty;
            var result = HttpStatusCode.BadRequest;

            try
            {
                var installedPackages = await GetInstalledPackagesAsync();

                if (installedPackages == null)
                {
                    // REST API error when getting installed packages
                    return false;
                }

                foreach (var app in installedPackages.Items)
                {
                    if (app.Name == appName)
                    {
                        url = AppTaskUrl + "app?package=" + RestHelper.EscapeUriString(app.PackageFullName);
                        break;
                    }
                    
                }
                if (String.IsNullOrEmpty(url))
                {
                    // App is not found in installed packages list
                    return false;
                }

                result = await this._restHelper.DeleteRequestAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result == HttpStatusCode.OK;
        }

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync()
        {
            const string URL = "/api/wifi/interfaces";

            using (var response = await this._restHelper.GetOrPostRequestAsync(URL, true))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
                }
            }

            return null;
        }

        public async Task<IPConfigurations> GetIpConfigurationsAsync()
        {
            const string URL = NetworkingApiUrl + "ipconfig";

            try
            {
                using (var response = await this._restHelper.GetOrPostRequestAsync(URL, true))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return RestHelper.ProcessJsonResponse(response, typeof(IPConfigurations)) as IPConfigurations;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return new IPConfigurations();
        }

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(string adapterName)
        {
            var url = "/api/wifi/networks?interface=" + adapterName.Trim("{}".ToCharArray());

            try
            {
                using (var response = await this._restHelper.GetOrPostRequestAsync(url, true))
                {
                    if (response == null)
                        return new AvailableNetworks();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return RestHelper.ProcessJsonResponse(response, typeof(AvailableNetworks)) as AvailableNetworks;
                    }
                }
            }
            catch(Exception wex)
            {
                // expected error
                Debug.WriteLine(wex);
            }

            return new AvailableNetworks();
        }

        public async Task<bool> ConnectToNetworkAsync(string adapterName, string ssid, string ssidPassword)
        {
            Dictionary<string, string> connectArguments = new Dictionary<string, string>()
            {
                { "interface", adapterName.Trim("{}".ToCharArray()) },
                { "ssid", RestHelper.EscapeUriString(ssid) },
                { "op", "connect" },
                { "createprofile", "yes" }
            };

            if (!string.IsNullOrEmpty(ssidPassword))
            {
                connectArguments.Add("key", RestHelper.EscapeUriString(ssidPassword));
            }

            Uri path = this._restHelper.CreateUri("/api/wifi/network", connectArguments);

            // "using" just to make sure the HttpWebResponse is disposed
            using (var response = await this._restHelper.GetOrPostRequestAsync(path, false))
            {
                return response.StatusCode == HttpStatusCode.OK;
            }
        }
    }
}
