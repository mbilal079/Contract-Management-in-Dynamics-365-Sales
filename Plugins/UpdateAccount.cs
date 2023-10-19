using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NGX.PLugins.UpdateAccountforContracts
{
    public class UpdateAccount : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Update Account plugin called");

            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                        (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {
                    Entity entity = (Entity)context.InputParameters["Target"];

                    if (entity.LogicalName != "mb_contract" && (context.MessageName != "Create" || context.MessageName != "Delete"))
                        return;

                    if(context.MessageName == "Create")
                    {
                        if (entity.Contains("mb_account"))
                        {
                            EntityReference account = (EntityReference)entity["mb_account"];
                            Entity accountEnt = service.Retrieve("account", account.Id, new ColumnSet("mb_noofcontracts"));
                            if (accountEnt.Contains("mb_noofcontracts"))
                            {
                                accountEnt["mb_noofcontracts"] = (int)accountEnt["mb_noofcontracts"] + 1;
                            }
                            else
                            {
                                accountEnt["mb_noofcontracts"] = 1;
                            }
                            accountEnt.Id = account.Id;
                            service.Update(accountEnt);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Exception => "+ex);
                }
            }
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
            {
                try
                {
                    EntityReference entityref = (EntityReference)context.InputParameters["Target"];

                    if (entityref.LogicalName != "mb_contract" && context.MessageName != "EntityReference")
                        return;

                    var PreImg = (context.PreEntityImages != null && context.PreEntityImages.Contains("PreImg")) ?
                        context.PreEntityImages["PreImg"] : null;
                    if (PreImg != null)
                    {
                        EntityReference account = null;
                        if (PreImg.Contains("mb_account"))
                        {
                            account = (EntityReference)PreImg["mb_account"];
                            // Define Condition Values
                            var query_mb_account = account.Id;

                            // Instantiate QueryExpression query
                            var query = new QueryExpression("mb_contract");

                            // Add columns to query.ColumnSet
                            query.ColumnSet.AddColumns("mb_account");

                            // Define filter query.Criteria
                            query.Criteria.AddCondition("mb_account", ConditionOperator.Equal, query_mb_account);
                            EntityCollection result = service.RetrieveMultiple(query);
                            int count = 0;
                            //foreach (var item in result.Entities)
                            //{
                            //    count += 1;
                            //    tracingService.Trace("in foreach");
                            //}
                            
                            //tracingService.Trace("Count =  "+count.ToString());
                            tracingService.Trace("Cout entities "+ result.Entities.Count);

                            Entity accountEnt = new Entity("account");
                            accountEnt["mb_noofcontracts"] = result.Entities.Count;


                            accountEnt.Id = account.Id;
                            service.Update(accountEnt);
                        }
                            
                    }
                }
                catch(Exception ex)
                {
                    tracingService.Trace("Exception in Delete: "+ex);
                }
            }
        }
    }
}
