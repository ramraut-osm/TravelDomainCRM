using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.IdentityModel.Metadata;

namespace MyPlugin
{
    public class PlaceAutopopulate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Extract the tracing service for use in debugging sandboxed plug-ins.  
            // If you are not registering the plug-in in the sandbox, then you do  
            // not have to add any tracing service related code.  
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                
                    try
                    {

                        string trNameValue = string.Empty;
                        DateTime startDate = DateTime.MinValue;

                        if (entity.Attributes.Contains("tr_startdate"))
                        {
                            startDate = ((DateTime)entity.Attributes["tr_startdate"]).Date;
                        }

                        if (entity.Attributes.Contains("tr_place")) 
                        { 
                                EntityReference placeData = (EntityReference)entity.Attributes["tr_place"];

                                Entity placeRecord = service.Retrieve(placeData.LogicalName, placeData.Id, new ColumnSet("tr_name"));
                                string placeLookupName = placeRecord.Contains("tr_name") ? placeRecord.GetAttributeValue<string>("tr_name") : "No Name";

                                string placeLogicalName = placeData.LogicalName;
                                Guid placeLookupId = placeData.Id;

                                trNameValue = $"{placeLookupName} - {startDate.ToString("M/d/yyyy")}";

                                entity.Attributes.Add("tr_name", trNameValue);
                        }
                    }

                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        throw new InvalidPluginExecutionException("An error occurred in MyPlugin.", ex);
                    }

                    catch (Exception ex)
                    {
                        tracingService.Trace("MyPlugin: {0}", ex.ToString());
                        throw;
                    }
            
            }
        }
    }
}
