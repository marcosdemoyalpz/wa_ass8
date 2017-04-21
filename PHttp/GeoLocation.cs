using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PHttp
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Class used to obtain Geo Locations from Client IP. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class GeoLocation
    {
        /// City Name
        public string city { get; private set; }
        /// Country Code
        public string countryc { get; private set; }
        /// Country Name
        public string countryn { get; private set; }
        /// Region Name
        public string region { get; private set; }
        /// Latitude
        public string lat { get; private set; }
        /// Longitude
        public string longi { get; private set; }
        /// Time Zone
        public string timez { get; private set; }
        /// Zip Code
        public string zip { get; private set; }
        /// ISP IP Address
        public string myIP { get; private set; }

        public GeoLocation(HttpRequestEventArgs e)
        {
            WebAPI(e);
        }
        private void WebAPI(HttpRequestEventArgs e)
        {
            #region Get Client IP
            string VisitorsIPAddr = string.Empty;
            if (e.Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                VisitorsIPAddr = e.Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
            }
            else if (e.Context.Request.UserHostAddress.Length != 0)
            {
                VisitorsIPAddr = e.Context.Request.UserHostAddress;
            }
            myIP = VisitorsIPAddr;
            #endregion

            #region Get Local Public IP
            //string url = "http://checkip.dyndns.org";
            //System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            //System.Net.WebResponse resp = req.GetResponse();
            //System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            //string response = sr.ReadToEnd().Trim();
            //string[] a = response.Split(':');
            //string a2 = a[1].Substring(1);
            //string[] a3 = a2.Split('<');
            //string a4 = a3[0];
            //myIP = a4;
            #endregion
            string strQuery;
            string key = "demo";
            HttpWebRequest HttpWReq;
            HttpWebResponse HttpWResp;
            strQuery = "http://api.ip2location.com" + "?ip=" + myIP + "&key=" + key + "&package=WS24&format=json";
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            HttpWReq = (HttpWebRequest)WebRequest.Create(strQuery);
            HttpWReq.Method = "GET";
            HttpWResp = (HttpWebResponse)HttpWReq.GetResponse();
            System.IO.StreamReader reader = new System.IO.StreamReader(HttpWResp.GetResponseStream());
            string content = reader.ReadToEnd();
            dynamic item = serializer.Deserialize<object>(content);

            Console.WriteLine("\n\t " + content);
            Console.WriteLine();
            foreach (var elem in item)
            {
                Console.WriteLine(elem);
            }

            city = item["city_name"];
            countryc = item["country_code"];
            countryn = item["country_name"];
            region = item["region_name"];
            lat = item["latitude"];
            longi = item["longitude"];
            timez = item["time_zone"];
            zip = item["zip_code"];
        }
    }
}
