using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace MyPlugin
{
    public class PlaceAutopopulate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {
                    Entity entity = (Entity)context.InputParameters["Target"];
                    AutopopulatePlaceAttributes(entity, service, tracingService);
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

        private void AutopopulatePlaceAttributes(Entity entity, IOrganizationService service, ITracingService tracingService)
        {
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
