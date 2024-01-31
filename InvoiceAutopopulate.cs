using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MyPlugin
{
    public class InvoiceAutopopulate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            // not have to add any tracing service related code.  
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.-
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Check if the plugin is executing in the context of the "Create" message and the target entity is "booking".
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


            if (context.InputParameters.Contains("Target") &&
                   context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                try
                {
                    EntityReference bookingData = (EntityReference)entity.Attributes["tr_bookingplan"];
                    Guid bookingLookupId = bookingData.Id;
                    Entity bookingRecord = service.Retrieve(bookingData.LogicalName, bookingData.Id, new ColumnSet("tr_primarycontact", "tr_cost"));

                    EntityReference primaryContactReference = bookingRecord.GetAttributeValue<EntityReference>("tr_primarycontact");
                    Guid primaryContactLookupId = primaryContactReference?.Id ?? Guid.Empty;
                    entity["tr_primarycontact"] = primaryContactReference;

                    Entity primaryContactRecord = service.Retrieve(primaryContactReference.LogicalName, primaryContactReference.Id, new ColumnSet("fullname"));
                    string primaryContactName = primaryContactRecord.GetAttributeValue<string>("fullname");

                    Money cost = bookingRecord.GetAttributeValue<Money>("tr_cost");
                    decimal costValue = cost?.Value ?? 0;
                    Money finalCostMoney = new Money(costValue);
                    entity.Attributes["tr_bookingcost"] = finalCostMoney;

                    if (entity.Attributes.Contains("tr_discountplan"))
                    {
                        EntityReference discountData = (EntityReference)entity.Attributes["tr_discountplan"];
                        Guid discountLookupId = discountData.Id;
                        Entity discountRecord = service.Retrieve(discountData.LogicalName, discountData.Id, new ColumnSet("tr_discountamount"));

                        if (discountRecord.Attributes.Contains("tr_discountamount"))
                        {
                            Money discountAmount = discountRecord.GetAttributeValue<Money>("tr_discountamount");
                            decimal discountValue = discountAmount?.Value ?? 0;
                            entity.Attributes.Add("tr_discountapplied", new Money(discountValue));
                            costValue -= discountValue;
                        }
                    }

                    decimal vatRate = entity.GetAttributeValue<decimal?>("tr_vatratenew").GetValueOrDefault();

                    if (vatRate != 0)
                    {
                        decimal vatValue = costValue * (vatRate / 100);
                        costValue += vatValue;
                    }

                    entity.Attributes["tr_total"] = new Money(costValue);

                    entity.Attributes.Add("tr_name", "INV-" + primaryContactName);
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
