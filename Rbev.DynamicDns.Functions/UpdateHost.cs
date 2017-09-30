using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using System.Collections.Generic;
using System.Net.Sockets;
using System.ServiceModel.Security;
using System.Web.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Rbev.DynamicDns.Functions.Framework;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Rbev.DynamicDns.Functions
{
    public static class UpdateHost
    {
        [FunctionName("UpdateHost")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "UpdateHost/{subscriptionId:guid}/{resourceGroup}/{zone}/{record}/{ipAddress}")]
            HttpRequestMessage req, 
            string subscriptionId,
            string resourceGroup,
            string zone,
            string record,
            string ipAddress,
            TraceWriter log)
        {
            //post content should be account secret
            var secret = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "detect")
            {
                ipAddress = req.GetClientHostAddress();
                log.Info($"No ip address provided, using client ip address: {ipAddress}");
            }

            var directoryId = WebConfigurationManager.AppSettings["DnsUpdateLoginAD"];
            var applicationid = WebConfigurationManager.AppSettings["DnsUpdateLoginApplication"];

            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(directoryId, new ClientCredential(applicationid, secret));
            var dnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = subscriptionId };

            if (!IPAddress.TryParse(ipAddress, out var ipAddressParsed))
            {
                log.Error("Failed to parse client ip");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Failed to parse ip address");
            }

            //https://docs.microsoft.com/en-us/azure/dns/dns-sdk
            var recordSetParams = new RecordSet();
            recordSetParams.TTL = 120;
            switch (ipAddressParsed.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    recordSetParams.ARecords = new List<ARecord>();
                    recordSetParams.ARecords.Add(new ARecord(ipAddress));
                    await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroup, zone, record, RecordType.A, recordSetParams);
                    return req.CreateResponse(HttpStatusCode.OK, $"Updated A record to {ipAddress}");
                case AddressFamily.InterNetworkV6:
                    recordSetParams.AaaaRecords = new List<AaaaRecord>();
                    recordSetParams.AaaaRecords.Add(new AaaaRecord(ipAddress));
                    await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroup, zone, record, RecordType.AAAA, recordSetParams);
                    return req.CreateResponse(HttpStatusCode.OK, $"Updated AAAA record to {ipAddress}");
                default:
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Unknown client ip address");
            }
        }

    }
}
