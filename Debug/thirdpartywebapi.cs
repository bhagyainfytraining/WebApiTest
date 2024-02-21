using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using System.Net;
using System.Net.Configuration;
using System.ServiceModel;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.Crm.Sdk;

namespace WebAPI
{
    public class thirdpartywebapi : IPlugin
    {

        private readonly string _configSettings;
        private readonly string _Key;
        private readonly string _url;

        public thirdpartywebapi(string configurationSettings)
        {
            if (!string.IsNullOrWhiteSpace(configurationSettings))
            {
                try
                {
                    _configSettings = configurationSettings;
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(_configSettings);
                    _Key = doc.SelectSingleNode("Data/key").InnerText;
                    _url = doc.SelectSingleNode("Data/url").InnerText;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Configs not found" + ex.InnerException.ToString());
                }
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context
            IPluginExecutionContext context = (IPluginExecutionContext)
              serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the IOrganizationService instance 
            IOrganizationServiceFactory serviceFactory =
              (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.UserId);

            // Obtain the Tracing service reference
            ITracingService tracingService =
              (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            //v1/latest?apikey=fca_live_CRJhnvG556YW0cpDyxCbv7YA1GYLntwVPurPaF3D
            try
            {
                tracingService.Trace("Insideplugin");
                var ent = (Entity)context.InputParameters["Target"];
                HttpClient client = new HttpClient();
                var query = $"v1/latest?apikey={_Key}";
                tracingService.Trace(_url + query);
                var request = (HttpWebRequest)WebRequest.Create(_url + query);

                request.Method = "GET";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                var content = string.Empty;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                }
                var parsedResponseJson = JsonObject.Parse(content);
                var CurrenciesJSON = parsedResponseJson["data"];
                var parsedCurrenciesJSOn = JsonObject.Parse(CurrenciesJSON.ToString());
                var USDTOAUD = parsedCurrenciesJSOn["AUD"];
                tracingService.Trace("USDTOAUD : " + USDTOAUD);

                // Entity task = new Entity();
                //task["regardingobjectid"] = new EntityReference("contact", ent.Id);
                ent["jobtitle1"] = "USDTOAUD : " + USDTOAUD;
                service.Update(ent);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException("The following error occurred in MyPlugin.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace("MyPlugin: error: {0}", ex.ToString());
                throw;
            }
        }
    }
}