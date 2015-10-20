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
        public static async Task<HttpWebResponse> MakeRequest(string requestUrl, string userName, string password)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Credentials = new NetworkCredential(userName, password);
                request.ContentLength = 0;

                Debug.WriteLine(string.Format("RestHelper: MakeRequest: url [{0}]", requestUrl));
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
               
                Debug.WriteLine(string.Format("RestHelper: MakeRequest: response code [{0}]", response.StatusCode));
                return response;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(string.Format("Error in MakeRequest, url [{0}]", requestUrl));
                Debug.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static object ProcessResponse(HttpWebResponse response, Type dataContractType)
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
            catch(Exception ex)
            {
                Debug.WriteLine(string.Format("Error in ProcessResponse, response [{0}]", responseContent));
                Debug.WriteLine(ex.ToString());
                throw ex;
            }
}
    }
}
