using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace MyPlugin
{
    public class InvoiceAutopopulate : IPlugin
    {
        private decimal costValue;
        private string primaryContactName; // Declare primaryContactName at the class level

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
                    HandleInvoiceAutopopulate(entity, service, tracingService);
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

        private void HandleInvoiceAutopopulate(Entity entity, IOrganizationService service, ITracingService tracingService)
        {
            Entity bookingRecord = RetrieveBookingRecord(entity, service);
            AutopopulateFromBooking(entity, bookingRecord, service);
            ApplyDiscount(entity, service);
            CalculateVAT(entity);
            entity.Attributes["tr_total"] = new Money(costValue);
            entity.Attributes.Add("tr_name", "INV-" + primaryContactName);
        }

        private Entity RetrieveBookingRecord(Entity entity, IOrganizationService service)
        {
            EntityReference bookingData = (EntityReference)entity.Attributes["tr_bookingplan"];
            return service.Retrieve(bookingData.LogicalName, bookingData.Id, new ColumnSet("tr_primarycontact", "tr_cost"));
        }

        private void AutopopulateFromBooking(Entity entity, Entity bookingRecord, IOrganizationService service)
        {
            EntityReference primaryContactReference = bookingRecord.GetAttributeValue<EntityReference>("tr_primarycontact");
            Guid primaryContactLookupId = primaryContactReference?.Id ?? Guid.Empty;
            //entity["tr_primarycontact"] = primaryContactReference;

            Entity primaryContactRecord = service.Retrieve(primaryContactReference.LogicalName, primaryContactReference.Id, new ColumnSet("fullname"));
            primaryContactName = primaryContactRecord.GetAttributeValue<string>("fullname");

            Money cost = bookingRecord.GetAttributeValue<Money>("tr_cost");
            costValue = cost?.Value ?? 0;
            Money finalCostMoney = new Money(costValue);
            entity.Attributes["tr_bookingcost"] = finalCostMoney;
        }

        private void ApplyDiscount(Entity entity, IOrganizationService service)
        {
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
        }

        private void CalculateVAT(Entity entity)
        {
            decimal vatRate = entity.GetAttributeValue<decimal?>("tr_vatratenew").GetValueOrDefault();

            if (vatRate != 0)
            {
                decimal vatValue = costValue * (vatRate / 100);
                costValue += vatValue;
            }
        }
    }
}
