using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.Helper
{
    public class RestHelper
    {
        public const string UrlFormat = "http://{0}:8080{1}";

        public static string Encode64(string toEncodeString)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncodeString.Trim());
            var string64 = Convert.ToBase64String(toEncodeAsBytes);

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

        public UserInfo DeviceAuthentication { get; private set; }

        public IPAddress IpAddress { get; private set; }

        public RestHelper(IPAddress ipAddress, UserInfo deviceAuthentication)
        {
            this.DeviceAuthentication = deviceAuthentication;
            this.IpAddress = ipAddress;
        }

        private enum HttpErrorResult { Fail, Retry, Cancel };

        private HttpErrorResult HandleError(WebException exception)
        {
            var errorResponse = exception.Response as HttpWebResponse;

            if (errorResponse?.StatusCode == HttpStatusCode.Unauthorized)
            {
                var dlg = new DialogAuthenticate(this.DeviceAuthentication);
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

        public async Task<HttpWebResponse> GetOrPostRequestAsync(string restPath, bool isGet)
        {
            var requestUrl = new Uri(string.Format(UrlFormat, this.IpAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            while (true)
            {
                try
                {
                    var request = WebRequest.Create(requestUrl) as HttpWebRequest;
                    if (request != null)
                    {
                        request.Method = isGet ? "Get" : "POST";
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
                    switch (HandleError(error))
                    {
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        case HttpErrorResult.Retry:
                            break;

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

            var password = isOneOffPassword ? passwordToUse : this.DeviceAuthentication.Password;

            while (true)
            {
                try
                {
                    var request = WebRequest.Create(requestUrl) as HttpWebRequest;

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
                        if (result == HttpStatusCode.OK)
                        {
                            var objStream = response.GetResponseStream();
                            
                            // tbd check for null objStream
                            var objReader = new StreamReader(objStream);

                            var respData = objReader.ReadToEnd();
                        }
                    }

                    return result;
                }
                catch (WebException error)
                {
                    if (isOneOffPassword)
                    {
                        throw;
                    }

                    switch (HandleError(error))
                    {
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl.AbsoluteUri}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        case HttpErrorResult.Retry:
                            break;

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
                        if (result == HttpStatusCode.OK)
                        {
                            var objStream = response.GetResponseStream();

                            // tbd check for null.

                            var objReader = new StreamReader(objStream);
                            string respData = objReader.ReadToEnd();
                        }
                    }

                    return result;
                }
                catch (WebException error)
                {
                    switch (HandleError(error))
                    {
                        case HttpErrorResult.Fail:
                            Debug.WriteLine($"Error in MakeRequest, url [{requestUrl.AbsolutePath}]");
                            Debug.WriteLine(error.ToString());
                            throw;

                        case HttpErrorResult.Retry:
                            break;

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
