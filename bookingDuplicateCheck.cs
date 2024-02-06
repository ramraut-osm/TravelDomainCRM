﻿using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace MyPlugin
{
    public class BookingDuplicateDetection : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    Entity entity = (Entity)context.InputParameters["Target"];
                    HandleBookingCreation(entity, service, tracingService);
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

        private void HandleBookingCreation(Entity entity, IOrganizationService service, ITracingService tracingService)
        {
            Entity packageRecord = RetrievePackageRecord(entity, service);

            SetBookingAttributes(entity, packageRecord);

            CheckForDuplicateBookings(entity, packageRecord, service);
        }

        private Entity RetrievePackageRecord(Entity entity, IOrganizationService service)
        {
            EntityReference packageData = (EntityReference)entity.Attributes["tr_packagename"];
            return service.Retrieve(packageData.LogicalName, packageData.Id, new ColumnSet("tr_name", "tr_startdate", "tr_packagecostpercouple", "tr_packagecostperchild", "tr_packagecostperday"));
        }

        private void SetBookingAttributes(Entity entity, Entity packageRecord)
        {
            string pck = packageRecord.GetAttributeValue<string>("tr_name");
            entity.Attributes.Add("tr_package", "BK-" + pck);

            DateTime startDate = packageRecord.GetAttributeValue<DateTime>("tr_startdate");
            entity.Attributes.Add("tr_bookingstartdate", startDate.Date);

            EntityReference contactData = (EntityReference)entity.Attributes["tr_primarycontact"];
            Guid contactLookupId = contactData.Id;

            Money adultRate = packageRecord.GetAttributeValue<Money>("tr_packagecostpercouple");
            Money childrenRate = packageRecord.GetAttributeValue<Money>("tr_packagecostperchild");
            Money daysRate = packageRecord.GetAttributeValue<Money>("tr_packagecostperday");

            int adultSelected = entity.GetAttributeValue<int>("tr_noofadults");
            int childrenSelected = entity.GetAttributeValue<int>("tr_noofchildren");
            int daysSelected = entity.GetAttributeValue<int>("tr_noofdays");

            decimal adultCost = adultSelected * adultRate.Value;
            decimal childrenCost = childrenSelected * childrenRate.Value;
            decimal daysCost = daysSelected * daysRate.Value;

            decimal totalCost = adultCost + childrenCost + daysCost;

            Console.WriteLine("Total Cost: " + totalCost);

            entity.Attributes.Add("tr_cost", new Money(totalCost));
        }

        private void CheckForDuplicateBookings(Entity entity, Entity packageRecord, IOrganizationService service)
        {
            var query = new QueryExpression("tr_bookings")
            {
                ColumnSet = new ColumnSet("tr_package"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("tr_packagename", ConditionOperator.Equal, packageRecord.Id),
                        new ConditionExpression("tr_primarycontact", ConditionOperator.Equal, entity.GetAttributeValue<EntityReference>("tr_primarycontact").Id),
                        new ConditionExpression("tr_bookingsid", ConditionOperator.NotEqual, entity.Id)
                    }
                }
            };

            EntityCollection duplicateBookings = service.RetrieveMultiple(query);

            if (duplicateBookings.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("A duplicate booking already exists for the same contact, package, and overlapping dates.");
            }
        }
    }
}
