using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DeviceCenter.DataContract;
using DeviceCenter.Helper;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Net.Http;

namespace DeviceCenter
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class WebBRest
    {
        private static readonly WebBRest s_Instance = new WebBRest();

        public static WebBRest Instance
        {
            get { return s_Instance; }
        }

        private const string DeviceApiUrl = "/api/iot/device/";
        private const string OsInfo = "/api/os/info";
        private const string RunCommandUrl = "/api/iot/processmanagement/runcommand";
        private const string ControlApiUrl = "/api/control/";
        private const string NetworkingApiUrl = "/api/networking/";
        private const string AppxApiUrl = "/api/appx/packagemanager/";
        private const string AppTaskUrl = "/api/taskmanager/";
        private const string PerfMgrUrl = "/api/resourcemanager/";

        private const int QueryInterval = 3000;

        // Used to manage REST call cancellation
        private CancellationTokenSource _tokenSource;
        private object _tokenLock;

        private bool _hasPendingRESTCall;

        [Serializable]
        public class RestError : Exception, ISerializable
        {
            public RestError(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        private WebBRest()
        {
            _tokenLock = new object();
            _hasPendingRESTCall = false;
        }

        public async Task<OsInfo> GetDeviceInfoAsync(DiscoveredDevice device)
        {
            string url = OsInfo;

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Get, string.Empty, cts))
                {
                    var data = RestHelper.ProcessJsonResponse(response, typeof(OsInfo)) as OsInfo;

                    // data might be null, caller should check
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                return null;
            }
        }

        public async Task<bool> SetDeviceNameAsync(DiscoveredDevice device, string newDeviceName)
        {
            string url = DeviceApiUrl + "name?newdevicename=";
            url += RestHelper.EscapeUriString(newDeviceName);

            CancellationToken? cts;

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            try
            {
                RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

                using (var result = await restHelper.SendRequestAsync(url, HttpMethod.Post, string.Empty, cts))
                {
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> SetPasswordAsync(DiscoveredDevice device, string oldPassword, string newPassword)
        {
            CancellationToken? cts;

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            var url = DeviceApiUrl + "password?";
            url = url + "oldpassword=" + RestHelper.EscapeUriString(oldPassword);
            url = url + "&newpassword=" + RestHelper.EscapeUriString(newPassword);

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            try
            {
                using (var result = await restHelper.SendRequestAsync(url, HttpMethod.Post, oldPassword, cts))
                {
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        // resaves the password
                        restHelper.DeviceAuthentication.Password = newPassword;
                        DialogAuthenticate.SavePassword(restHelper.DeviceAuthentication);
                        return true;
                    }
                }
            }
            catch(UnauthorizedAccessException ex)
            {
                Debug.WriteLine(ex.Message);

                if(_hasPendingRESTCall)
                {
                    MessageBox.Show(
                    Strings.Strings.MessageBadPassword,
                    LocalStrings.AppNameDisplay,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                    return false;
                }
            }
            catch (Exception ex)
            {
                var dlg = new WindowError()
                {
                    Header = Strings.Strings.TitleDeviceAttributeChangeError,
                    Message = Strings.Strings.DeviceAttributeChangeErrorMessage,
                    HelpLink = new Uri("http://go.microsoft.com/fwlink/?LinkID=722169"),
                    HelpLinkText = Strings.Strings.DeviceAttributeChangeErrorHelpLink,
                    Owner = Window.GetWindow(Application.Current.MainWindow)
                };
                dlg.ShowDialog();
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        public async Task<WebExceptionStatus> RestartAsync(DiscoveredDevice device)
        {
            var url = ControlApiUrl + "restart";

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            WebExceptionStatus status = WebExceptionStatus.Success;

            CancellationToken? cts;

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            try
            {
                using (var result = await restHelper.SendRequestAsync(url, HttpMethod.Post, string.Empty, cts))
                {
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        return status;
                    }
                    else
                    {
                        return WebExceptionStatus.ProtocolError;
                    }
                }
            }
            catch (WebException webException)
            {
                Debug.WriteLine(webException.Message);
                return webException.Status;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return WebExceptionStatus.UnknownError;
            }
        }

        public async Task<DateTime?> GetDateTimeAsync(DiscoveredDevice device)
        {
            CancellationToken? cts;

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            var url = string.Format("{0}datetime", DeviceApiUrl);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Get, null, cts))
                {
                    var dateTime = RestHelper.ProcessJsonResponse(response, typeof(CurrentDateTime)) as CurrentDateTime;

                    return dateTime.Current.DateTime;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public async Task<bool> RunCommandAsync(DiscoveredDevice device, string command, bool runAsDefaultAccount)
        {
            CancellationToken? cts;

            if (string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            // If there is a REST call being made, this aborts the connection
            EnterWebBCall(out cts);

            Dictionary<string, string> commandArguments = new Dictionary<string, string>()
            {
                { "command", RestHelper.EscapeUriString(command) },
                { "runasdefaultaccount", runAsDefaultAccount ? RestHelper.EscapeUriString("true") : RestHelper.EscapeUriString("false") }
            };

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            var url = restHelper.CreateUri(RunCommandUrl, commandArguments);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Post, null, cts))
                {
                    if(response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        public async Task<bool> RunAppxAsync(DiscoveredDevice device, string packageFullName, IEnumerable<FileInfo> files)
        {
            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);
            
            bool isDeployed = false;
            bool deploymentSuccess = false;

            InstalledPackages installedPackages;

            installedPackages = await this.GetInstalledPackagesAsync(device);

            if (installedPackages != null)
            {
                foreach (var package in installedPackages.Items)
                {
                    if (package.PackageFullName.Equals(packageFullName))
                    {
                        isDeployed = true;
                        break;
                    }
                }
            }

            // If for some reason we failed to get the installed packages
            // assume it is not deployed

            if (!isDeployed)
            {
                var url = AppxApiUrl + "package?package=";
                url += files.First().Name;

                var result = HttpStatusCode.BadRequest;

                CancellationToken? cts;

                EnterWebBCall(out cts);

                try
                {
                    using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Post, null, files, cts))
                    {
                        result = response.StatusCode;

                        using (var stream = response.GetResponseStream())
                        {
                            if (stream != null)
                            {
                                using (var sr = new StreamReader(stream))
                                {
                                    Debug.WriteLine(await sr.ReadToEndAsync());
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (result == HttpStatusCode.Accepted)
                {
                    deploymentSuccess = await PollInstallStateAsync(device);
                }
            }

            if (!isDeployed && !deploymentSuccess)
            {
                return false;
            }

            return await StartAppAsync(device, packageFullName);
        }

        public async Task<bool> PollInstallStateAsync(DiscoveredDevice device)
        {
            const string URL = AppxApiUrl + "state";
            var result = HttpStatusCode.BadRequest;

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            while (result != HttpStatusCode.NotFound && result != HttpStatusCode.OK)
            {
                EnterWebBCall(out cts);

                try
                {
                    using (var response = await restHelper.SendRequestAsync(URL, HttpMethod.Get, string.Empty, cts))
                    {
                        result = response.StatusCode;
                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            await Task.Delay(QueryInterval);
                        }
                        else
                        {
                            var state = RestHelper.ProcessJsonResponse(response, typeof(DeploymentState)) as DeploymentState;

                            if (state != null)
                            {
                                if (state.IsSuccess)
                                {
                                    return true;
                                }
                                else
                                {
                                    // This throws a COMException
                                    Marshal.ThrowExceptionForHR(state.HResult);
                                }
                            }
                        }
                    }
                }
                catch (COMException ex)
                {
                    // I don't like to show a message box directly from here, but this call is nested in other call,
                    // and that call catch all the exceptions and return false, because of that we loose the localized
                    // exception message
                    var errorCaption = LocalStrings.AppNameDisplay;
                    var errorMsg = ex.Message;

                    // The message in the exception is localized
                    MessageBox.Show(errorMsg, errorCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    Debug.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return false;
        }

        public async Task<InstalledPackages> GetInstalledPackagesAsync(DiscoveredDevice device)
        {
            var url = AppxApiUrl + "packages";

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Get, string.Empty, cts))
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

        public async Task<bool> IsAppRunning(DiscoveredDevice device, string packageFullName)
        {
            const string URL = PerfMgrUrl + "processes";

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(URL, HttpMethod.Get, string.Empty, cts))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException();
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var runningProcesses = RestHelper.ProcessJsonResponse(response, typeof(IoTProcesses)) as IoTProcesses;
                        if (runningProcesses != null && runningProcesses.Items.Any(runningProcess => runningProcess.PackageFullName == packageFullName))
                        {
                            return true;
                        }
                    }
                }
                    
            }
            catch(Exception ex)
            {
                throw new RestError(ex.Message, ex);
            }

            return false;
        }

        public async Task<bool> StartAppAsync(DiscoveredDevice device, string packageFullName)
        {
            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);


            // Does not throw
            var installedPackages = await GetInstalledPackagesAsync(device);

            if (installedPackages == null)
            {
                // REST API error when getting installed packages
                return false;
            }

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                foreach (var app in installedPackages.Items)
                {
                    if (app.PackageFullName == packageFullName)
                    {
                        var url = AppTaskUrl + "app?appid=" + RestHelper.EscapeUriString(app.PackageRelativeId)
                                     + "&package=" + RestHelper.EscapeUriString(app.PackageFullName);

                        using (var result = await restHelper.SendRequestAsync(url, HttpMethod.Post, string.Empty, cts))
                        {
                            return (result.StatusCode == HttpStatusCode.OK);
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

        public async Task<bool> StopAppAsync(DiscoveredDevice device, string packageFullName)
        {
            var url = String.Empty;
            var result = HttpStatusCode.BadRequest;

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                url = AppTaskUrl + "app?package=" + RestHelper.EscapeUriString(packageFullName);

                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Delete, null, cts))
                {
                    result = response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result == HttpStatusCode.OK;
        }

        public async Task<WirelessAdapters> GetWirelessAdaptersAsync(DiscoveredDevice device)
        {
            const string URL = "/api/wifi/interfaces";

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(URL, HttpMethod.Get, string.Empty, cts))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return RestHelper.ProcessJsonResponse(response, typeof(WirelessAdapters)) as WirelessAdapters;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return new WirelessAdapters();
        }

        public async Task<IPConfigurations> GetIpConfigurationsAsync(DiscoveredDevice device)
        {
            const string URL = NetworkingApiUrl + "ipconfig";

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(URL, HttpMethod.Get, string.Empty, cts))
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

        public async Task<AvailableNetworks> GetAvaliableNetworkAsync(DiscoveredDevice device, string adapterName)
        {
            var url = "/api/wifi/networks?interface=" + adapterName.Trim("{}".ToCharArray());

            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

            try
            {
                using (var response = await restHelper.SendRequestAsync(url, HttpMethod.Get, string.Empty, cts))
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

        public async Task<bool> ConnectToNetworkAsync(DiscoveredDevice device, string adapterName, string ssid, string ssidPassword)
        {
            RestHelper restHelper = new RestHelper(null, device.IpAddress, device.Authentication);

            CancellationToken? cts;

            EnterWebBCall(out cts);

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

            try
            {
                Uri path = restHelper.CreateUri("/api/wifi/network", connectArguments);

                using (var response = await restHelper.SendRequestAsync(path, HttpMethod.Post, string.Empty, cts))
                {
                    return (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch(WebException webEx)
            {
                Debug.WriteLine(webEx.Message);
                throw webEx;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        public void TerminateAnyWebBCall()
        {
            lock (_tokenLock)
            {
                if (_tokenSource != null)
                {
                    InvalidateToken();
                }
            }
        }

        // BeginWebBCall and EndWebBCall have two purposes:
        // 1: Not allow two WebB calls to be made at the same time, no matter if the target of the calls are different
        // 2: Manage the cancellation token
        // 
        // Now this method checks if there is an active CancellationTokenSource and call cancel on it
        // It is not needed to call LeaveWebBCall() anymore because of that
        private void EnterWebBCall(out CancellationToken? cts)
        {
            lock (_tokenLock)
            {
                Debug.WriteLine("Starting WebB call...");

                // If there is a active REST call, cancel it
                // InvalidateToken() is not blocking, so it is possible
                // that the next WebB call will be blocked for a while
                // in GetResponseAsync() if there are more than two concurrent connections
                // to a device, however it shouldn't take long to unblock since the previous
                // connections are being aborted.
                if (_tokenSource != null)
                {
                    InvalidateToken();
                }

                _tokenSource = new CancellationTokenSource();
                cts = _tokenSource.Token;

                _hasPendingRESTCall = true;
            }
        }

        private void InvalidateToken()
        {
            Debug.WriteLine("Ending WebB call...");

            _hasPendingRESTCall = false;

            // Issue a cancel. Can't dispose here because the HttpCancellationHelper is still holding on to this.  
            // Expect it to be disposed by garbage collector.
            _tokenSource.Cancel();
            _tokenSource = null;
        }
    }
}
