using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public UserInfo DeviceAuthentication { get; private set; }
        public IPAddress IPAddress { get; private set; }

        public RestHelper(IPAddress ipAddress, UserInfo deviceAuthentication)
        {
            this.DeviceAuthentication = deviceAuthentication;
            this.IPAddress = ipAddress;
        }

        private enum HttpErrorResult { fail, retry, cancel };
        private HttpErrorResult HandleError(WebException exception)
        {
            HttpWebResponse errorResponse = exception.Response as HttpWebResponse;

            if (errorResponse != null)
            {
                if (errorResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    DialogAuthenticate dlg = new DialogAuthenticate(this.DeviceAuthentication);
                    bool? dlgResult = dlg.ShowDialog();

                    if (dlgResult.HasValue && dlgResult.Value)
                        return HttpErrorResult.retry;

                    return HttpErrorResult.cancel;
                }
            }

            return HttpErrorResult.fail;
        }

        public Uri CreateUri(string restPath)
        {
            return new Uri(string.Format(UrlFormat, this.IPAddress.ToString(), restPath), UriKind.Absolute);
        }

        public async Task<HttpWebResponse> GetOrPostRequestAsync(string restPath, bool isGet)
        {
            Uri requestUrl = new Uri(string.Format(UrlFormat, this.IPAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            bool running = true;
            while (running)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                    request.Method = isGet ? "Get" : "POST";
                    request.ContentLength = 0;
                    request.KeepAlive = false;
                    string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(DeviceAuthentication.UserName + ":" + DeviceAuthentication.Password));
                    request.Headers.Add("Authorization", "Basic " + encodedAuth);

                    Debug.WriteLine(string.Format("RestHelper: MakeRequest: url [{0}]", requestUrl));
                    HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                    Debug.WriteLine(string.Format("RestHelper: MakeRequest: response code [{0}]", response.StatusCode));
                    return response;
                }
                catch (WebException error)
                {
                    switch (HandleError(error))
                    {
                        case HttpErrorResult.fail:
                            Debug.WriteLine(string.Format("Error in MakeRequest, url [{0}]", requestUrl));
                            Debug.WriteLine(error.ToString());
                            Debug.Fail("Debug break");
                            throw error;
                        case HttpErrorResult.retry:
                            break;
                        case HttpErrorResult.cancel:
                            // todo: can caller handle this?
                            return error.Response as HttpWebResponse;
                    }
                }
            }

            return null; // should never get here
        }

        public async Task<HttpStatusCode> PostRequestAsync(string restPath)
        {
            Stream objStream = null;
            StreamReader objReader = null;
            HttpStatusCode result = HttpStatusCode.BadRequest;

            Uri requestUrl = new Uri(string.Format(UrlFormat, this.IPAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsoluteUri);

            bool running = true;
            while (running)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;

                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.DeviceAuthentication.UserName + ":" + this.DeviceAuthentication.Password));
                    request.Headers.Add("Authorization", "Basic " + encodedAuth);
                    request.ContentLength = 0;

                    HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                    result = response.StatusCode;
                    if (result == HttpStatusCode.OK)
                    {
                        objStream = response.GetResponseStream();
                        objReader = new StreamReader(objStream);
                        string respData = objReader.ReadToEnd();
                    }

                    return result;
                }
                catch (WebException error)
                {
                    switch (HandleError(error))
                    {
                        case HttpErrorResult.fail:
                            Debug.WriteLine(string.Format("Error in MakeRequest, url [{0}]", requestUrl.AbsoluteUri));
                            Debug.WriteLine(error.ToString());
                            Debug.Fail("Debug break");
                            throw error;
                        case HttpErrorResult.retry:
                            break;
                        case HttpErrorResult.cancel:
                            // todo: can caller handle this?
                            return (error.Response as HttpWebResponse).StatusCode;
                    }
                }
            }

            return result;
        }

        public async Task<HttpStatusCode> DeleteRequestAsync(string restPath)
        {
            Stream objStream = null;
            StreamReader objReader = null;
            HttpStatusCode result = HttpStatusCode.BadRequest;

            Uri requestUrl = new Uri(string.Format(UrlFormat, this.IPAddress.ToString(), restPath), UriKind.Absolute);
            Debug.WriteLine(requestUrl.AbsolutePath);

            bool running = true;
            while (running)
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

                    HttpWebResponse response = (HttpWebResponse)(await req.GetResponseAsync());
                    result = response.StatusCode;
                    if (result == HttpStatusCode.OK)
                    {
                        objStream = response.GetResponseStream();
                        objReader = new StreamReader(objStream);
                        string respData = objReader.ReadToEnd();
                    }

                    return result;
                }
                catch (WebException error)
                {
                    switch (HandleError(error))
                    {
                        case HttpErrorResult.fail:
                            Debug.WriteLine(string.Format("Error in MakeRequest, url [{0}]", requestUrl.AbsolutePath));
                            Debug.WriteLine(error.ToString());
                            Debug.Fail("Debug break");
                            throw error;
                        case HttpErrorResult.retry:
                            break;
                        case HttpErrorResult.cancel:
                            // todo: can caller handle this?
                            return (error.Response as HttpWebResponse).StatusCode;
                    }
                }
            }

            return result;
        }

        public static object ProcessJsonResponse(HttpWebResponse response, Type dataContractType)
        {
            string responseContent = string.Empty;
            try
            {
                var objStream = response.GetResponseStream();
                var sr = new StreamReader(objStream);
                responseContent = sr.ReadToEnd();
                byte[] byteArray = Encoding.UTF8.GetBytes(responseContent);
                MemoryStream stream = new MemoryStream(byteArray);
                var serializer = new DataContractJsonSerializer(dataContractType);
                object jsonObj = serializer.ReadObject(stream);
                if (jsonObj != null)
                {
                    Debug.WriteLine(jsonObj.ToString());
                }

                return jsonObj;
            }
            catch (SerializationException ex)
            {
                Debug.WriteLine(string.Format("Error in ProcessResponse, response [{0}]", responseContent));
                Debug.WriteLine(ex.ToString());

                return Activator.CreateInstance(dataContractType); // return a blank instance
            }
        }
    }
}
