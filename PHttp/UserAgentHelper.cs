using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace PHttp
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Parse User Agents Class. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class UserAgentHelper
    {
        public string agent_type { get; private set; }
        public string agent_name { get; private set; }
        public string agent_version { get; private set; }
        public string os_type { get; private set; }
        public string os_name { get; private set; }
        public string os_versionName { get; private set; }
        public string os_versionNumber { get; private set; }
        public string linux_distibution { get; private set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">    HTTP request event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public UserAgentHelper(HttpRequestEventArgs e)
        {
            agent_type = "";
            agent_name = "";
            agent_version = "";
            os_type = "";
            os_name = "";
            os_versionName = "";
            os_versionNumber = "";
            linux_distibution = "";
            WebAPI(e);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process UserAgent. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">    HTTP request event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void WebAPI(HttpRequestEventArgs e)
        {
            string userAgent = e.Request.UserAgent;
            string strQuery;
            string key = "demo";
            HttpWebRequest HttpWReq;
            HttpWebResponse HttpWResp;
            strQuery = "http://www.useragentstring.com/?uas=" + HttpUtility.UrlEncode(userAgent) + "&key=" + key + "&getJSON=all";
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            HttpWReq = (HttpWebRequest)WebRequest.Create(strQuery);
            HttpWReq.Method = "GET";
            HttpWResp = (HttpWebResponse)HttpWReq.GetResponse();
            System.IO.StreamReader reader = new System.IO.StreamReader(HttpWResp.GetResponseStream());
            string content = reader.ReadToEnd();
            dynamic item = serializer.Deserialize<object>(content);
            agent_type = item["agent_type"];
            agent_name = item["agent_name"];
            agent_version = item["agent_version"];
            os_type = item["os_type"];
            os_name = item["os_name"];
            os_versionName = item["os_versionName"];
            os_versionNumber = item["os_versionNumber"];
            linux_distibution = item["linux_distibution"];
        }
    }
}
