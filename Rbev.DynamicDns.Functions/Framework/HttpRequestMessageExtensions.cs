using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;

namespace Rbev.DynamicDns.Functions.Framework
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetClientHostAddress(this HttpRequestMessage request)
        {
            //azure hosted
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            //locally hosted
            if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                var prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }

            return null;
        }
    }
}