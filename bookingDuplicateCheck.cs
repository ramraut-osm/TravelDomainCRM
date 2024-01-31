using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Services;
using System.ServiceModel;
using System.Runtime.InteropServices.ComTypes;

namespace MyPlugin
{
     public class BookingDuplicateDetection : IPlugin
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
                        EntityReference packageData = (EntityReference)entity.Attributes["tr_packagename"];
                        Guid packageLookupId = packageData.Id;
                        Entity packageRecord = service.Retrieve(packageData.LogicalName, packageData.Id, new ColumnSet("tr_name", "tr_startdate", "tr_packagecostpercouple", "tr_packagecostperchild", "tr_packagecostperday"));
                   

                        string pck = packageRecord.GetAttributeValue<string>("tr_name");
                        entity.Attributes.Add("tr_package", "BK-" + pck);

                        DateTime startDate = packageRecord.GetAttributeValue<DateTime>("tr_startdate");
                        entity.Attributes.Add("tr_bookingstartdate", startDate.Date);

                        EntityReference contactData = (EntityReference)entity.Attributes["tr_primarycontact"];
                        //Entity contactRecord = service.Retrieve(contactData.LogicalName, contactData.Id, new ColumnSet("fullname"));
                        //string cnt = contactRecord.GetAttributeValue<string>("fullname");
                        Guid contactLookupId = contactData.Id;

                        Money adultRate = packageRecord.GetAttributeValue<Money>("tr_packagecostpercouple");
                        Money childrenRate = packageRecord.GetAttributeValue<Money>("tr_packagecostperchild");
                        Money daysRate = packageRecord.GetAttributeValue<Money>("tr_packagecostperday");

                        int adultSelected= 0;
                        int childrenSelected = 0;
                        int daysSelected = 0;

                        if (entity.Attributes.Contains("tr_noofadults"))
                        {
                            adultSelected = entity.GetAttributeValue<int>("tr_noofadults");
                        }

                        if (entity.Attributes.Contains("tr_noofchildren"))
                        {
                            childrenSelected = entity.GetAttributeValue<int>("tr_noofchildren");
                        }

                        if (entity.Attributes.Contains("tr_noofdays"))
                        {
                            daysSelected = entity.GetAttributeValue<int>("tr_noofdays");
                        }

                        decimal adultCost = adultSelected * adultRate.Value;
                        decimal childrenCost = childrenSelected * childrenRate.Value;
                        decimal daysCost = daysSelected * daysRate.Value;

                        decimal totalCost = adultCost + childrenCost + daysCost;

                        Console.WriteLine("Total Cost: " + totalCost);

                        entity.Attributes.Add("tr_cost", new Money(totalCost));


                        var query = new QueryExpression("tr_bookings")
                            {
                                ColumnSet = new ColumnSet("tr_package"),
                                Criteria =
                                {
                                     Conditions =
                                     {
                                         new ConditionExpression("tr_packagename", ConditionOperator.Equal, packageLookupId),
                                         new ConditionExpression("tr_primarycontact", ConditionOperator.Equal, contactLookupId),
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

