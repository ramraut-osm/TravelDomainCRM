using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace MyPlugin
{
    public class DuplicateCheck : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
             
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

              
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);



              
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity booking = (Entity)context.InputParameters["Target"];


                try
                {

                    //email = contact.Attributes["emailaddress1"].ToString();

                    //// select * from contact where emailaddress1 == 'email'

                    //QueryExpression query = new QueryExpression("contact");
                    //query.ColumnSet = new ColumnSet(new string[] { "emailaddress1" });
                    //query.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, email);

                    EntityReference contactRef = booking.GetAttributeValue<EntityReference>("tr_primarycontact");
                    EntityReference packageRef = booking.GetAttributeValue<EntityReference>("tr_packagename");

                    Entity packageEntity = service.Retrieve(packageRef.LogicalName, packageRef.Id, new ColumnSet("tr_startdate", "tr_enddate"));
                    DateTime startDate = packageEntity.GetAttributeValue<DateTime>("tr_startdate");
                    DateTime endDate = packageEntity.GetAttributeValue<DateTime>("tr_enddate");

                    var query = new QueryExpression("booking")
                    {
                        ColumnSet = new ColumnSet("bookingid"),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("contact", ConditionOperator.Equal, contactRef.Id),
                                new ConditionExpression("package", ConditionOperator.Equal, packageRef.Id),
                                //new ConditionExpression("start_date", ConditionOperator.OnOrAfter, startDate),
                                //new ConditionExpression("end_date", ConditionOperator.OnOrBefore, endDate),
                                new ConditionExpression("bookingid", ConditionOperator.NotEqual, booking.Id)
                            }
                        }
                    };

                    EntityCollection duplicateBookings = service.RetrieveMultiple(query);

                    if (duplicateBookings.Entities.Count > 0)
                    {
                        throw new InvalidPluginExecutionException("A duplicate booking already exists for the same contact, package, and dates.");
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in MyPlug-in.", ex);
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
