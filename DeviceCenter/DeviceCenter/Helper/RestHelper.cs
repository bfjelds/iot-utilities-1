using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DeviceCenter.Helper
{
    public class RestHelper
    {
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

        public static async Task<HttpWebResponse> MakeRequest(string requestUrl, bool isGet, string userName, string password)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                request.Method = isGet ? "Get" : "POST";
                request.ContentLength = 0;
                string encodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(userName + ":" + password));
                request.Headers.Add("Authorization", "Basic " + encodedAuth);

                Debug.WriteLine(string.Format("RestHelper: MakeRequest: url [{0}]", requestUrl));
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                Debug.WriteLine(string.Format("RestHelper: MakeRequest: response code [{0}]", response.StatusCode));
                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Error in MakeRequest, url [{0}]", requestUrl));
                Debug.WriteLine(ex.ToString());
                Debug.Fail("Debug break");
                throw ex;
            }
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
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Error in ProcessResponse, response [{0}]", responseContent));
                Debug.WriteLine(ex.ToString());
                Debug.Fail("Debug break");
                throw ex;
            }
        }
    }
}
