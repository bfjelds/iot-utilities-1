using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeviceCenter.Helper
{
    public class RestHelper
    {
        public const string UrlFormat = "http://{0}:8080{1}";

        public static string EscapeUriString(string toEncodeString)
        {
            return Uri.EscapeUriString(Convert.ToBase64String(Encoding.ASCII.GetBytes(toEncodeString.Trim())));
        }

        public UserInfo DeviceAuthentication { get; private set; }

        public IPAddress IpAddress { get; private set; }

        private Window _parent;

        public RestHelper(Window parent, IPAddress ipAddress, UserInfo deviceAuthentication)
        {
            this.DeviceAuthentication = deviceAuthentication;
            this.IpAddress = ipAddress;
            this._parent = parent;
        }

        private enum HttpErrorResult { Fail, Retry, Cancel };

        private HttpErrorResult HandleError(WebException exception)
        {
            var errorResponse = exception.Response as HttpWebResponse;

            if (errorResponse?.StatusCode == HttpStatusCode.Unauthorized)
            {
                var dlg = new DialogAuthenticate(this.DeviceAuthentication);
                dlg.Owner = this._parent;

                var dlgResult = dlg.ShowDialog();

                if (dlgResult.HasValue && dlgResult.Value)
                    return HttpErrorResult.Retry;

                return HttpErrorResult.Cancel;
            }

            return HttpErrorResult.Fail;
        }

        public Uri CreateUri(string restPath)
        {
            return new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
        }

        public Uri CreateUri(string restPath, Dictionary<string, string> arguments)
        {
            bool first = true;
            StringBuilder argumentString = new StringBuilder();

            foreach (var cur in arguments)
            {
                if (first)
                    first = false;
                else
                    argumentString.Append("&");

                argumentString.Append(cur.Key);
                argumentString.Append("=");
                argumentString.Append(cur.Value);
            }

            return new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath) + "?" + argumentString.ToString(), UriKind.Absolute);
        }

        public async Task<HttpWebResponse> GetOrPostRequestAsync(string restPath, bool isGet)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            return await GetOrPostRequestAsync(requestUrl, isGet);
        }

        public async Task<HttpWebResponse> GetOrPostRequestAsync(Uri requestUrl, bool isGet)
        {
            while (true)
            {
                try
                {
                    var request = WebRequest.Create(requestUrl) as HttpWebRequest;
                    if (request != null)
                    {
                        request.Method = isGet ? "GET" : "POST";
                        request.ContentLength = 0;
                        request.KeepAlive = false;
                        var encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(DeviceAuthentication.UserName + ":" + DeviceAuthentication.Password));
                        request.Headers.Add("Authorization", "Basic " + encodedAuth);

                        Debug.WriteLine($"RestHelper: MakeRequest: url [{requestUrl}]");
                        var response = await request.GetResponseAsync() as HttpWebResponse;

                        if (response != null)
                        {
                            Debug.WriteLine($"RestHelper: MakeRequest: response code [{response.StatusCode}]");

                            // WebB let you try to authenticate three times, after that it will redirect you
                            // to the URL bellow. If we don't check this it will seem like the REST call was a success
                            // and we will fail in the JSON parsing, leaving no feedback for the user.
                            if(response.ResponseUri.AbsolutePath.ToUpper().Equals("/AUTHORIZATIONREQUIRED.HTM"))
                            {
                                // Free connection resources
                                response.Dispose();

                                // Keep trying to authenticate
                                continue;
                            }

                            return response;
                        }
                        else
                        {
                            // tbd what to do?
                            return null;
                        }
                    }
                }
                catch (WebException error)
                {
                    // HandleError() shows the authentication dialog box in case the WebException status code
                    // is HttpStatusCode.Unauthorized
                    switch (HandleError(error))
                    {
                        // Pass exception to the caller
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        // Keep going with the while loop
                        case HttpErrorResult.Retry:
                            break;

                        // Return HttpWebResponse to the caller
                        case HttpErrorResult.Cancel:
                            // todo: can caller handle this?
                            return error.Response as HttpWebResponse;
                    }
                }
            }
        }

        /// <summary>
        /// Post async request to WebB.
        /// </summary>
        /// <param name="restPath">path to REST api</param>
        /// <param name="passwordToUse">password to use for the REST api if specified, used when changing old password.</param>
        /// <returns></returns>
        public async Task<HttpStatusCode> PostRequestAsync(string restPath, string passwordToUse)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            // true when it's not using the password the app remembers.  this is used by set password page with oldpassword information.
            var isOneOffPassword = !string.IsNullOrEmpty(passwordToUse);

            while (true)
            {
                try
                {
                    var request = WebRequest.Create(requestUrl) as HttpWebRequest;

                    // This should go here, otherwise we are not going to use the most up-to-date password
                    // provided by the user in the dialog box
                    var password = isOneOffPassword ? passwordToUse : this.DeviceAuthentication.Password;

                    // Tbd check for NULL.
                    request.Method = "POST";

                    request.ContentType = "application/x-www-form-urlencoded";
                    string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.DeviceAuthentication.UserName + ":" + password));
                    request.Headers.Add("Authorization", "Basic " + encodedAuth);
                    request.ContentLength = 0;

                    var result = HttpStatusCode.BadRequest;
                    using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                    {
                        result = response.StatusCode;
                    }

                    return result;
                }
                catch (WebException error)
                {
                    if (isOneOffPassword)
                    {
                        throw;
                    }

                    // HandleError() shows the authentication dialog box in case the WebException status code
                    // is HttpStatusCode.Unauthorized
                    switch (HandleError(error))
                    {
                        // Pass exception to the caller
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl.AbsoluteUri}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        // Keep going with the while loop
                        case HttpErrorResult.Retry:
                            break;

                        // Return HttpStatusCode.Unauthorized to the caller
                        case HttpErrorResult.Cancel:
                            var httpWebResponse = error.Response as HttpWebResponse;
                            if (httpWebResponse != null)
                            {
                                return httpWebResponse.StatusCode;
                            }
                            else
                            {
                                throw;
                            }
                    }
                }
            }
        }

        public async Task<HttpStatusCode> DeleteRequestAsync(string restPath)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsolutePath);

            while (true)
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUrl);
                    req.Method = "DELETE";
                    req.ContentType = "application/x-www-form-urlencoded";
                    req.KeepAlive = false;
                    string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.DeviceAuthentication.UserName + ":" + this.DeviceAuthentication.Password));
                    req.Headers.Add("Authorization", "Basic " + encodedAuth);
                    req.ContentLength = 0;

                    var result = HttpStatusCode.BadRequest;
                    using (var response = (HttpWebResponse)(await req.GetResponseAsync()))
                    {
                        result = response.StatusCode;
                    }

                    return result;
                }
                catch (WebException error)
                {
                    // HandleError() shows the authentication dialog box in case the WebException status code
                    // is HttpStatusCode.Unauthorized
                    switch (HandleError(error))
                    {
                        // Pass exception to the caller
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl.AbsolutePath}]");
                            Debug.WriteLine(error.ToString());
                            throw;
                        // Keep going with the while loop
                        case HttpErrorResult.Retry:
                            break;

                        // Return HttpStatusCode.Unauthorized to the caller
                        case HttpErrorResult.Cancel:
                            var httpWebResponse = error.Response as HttpWebResponse;
                            if (httpWebResponse != null)
                            {
                                return httpWebResponse.StatusCode;
                            }
                            else
                            {
                                throw;
                            }
                    }
                }
            }
        }

        public static object ProcessJsonResponse(HttpWebResponse response, Type dataContractType)
        {
            var responseContent = string.Empty;
            try
            {
                var objStream = response.GetResponseStream();
                
                // tbd check for NULL objStream
                var sr = new StreamReader(objStream);

                responseContent = sr.ReadToEnd();
                var byteArray = Encoding.UTF8.GetBytes(responseContent);
                var stream = new MemoryStream(byteArray);
                var serializer = new DataContractJsonSerializer(dataContractType);
                var jsonObj = serializer.ReadObject(stream);

                if (jsonObj != null)
                {
                    Debug.WriteLine(jsonObj.ToString());
                }

                return jsonObj;
            }
            catch (SerializationException ex)
            {
                Debug.WriteLine($"Error in ProcessResponse, response [{responseContent}]");
                Debug.WriteLine(ex.ToString());

                return Activator.CreateInstance(dataContractType); // return a blank instance
            }
        }
    }
}
