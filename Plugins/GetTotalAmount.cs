using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NGX.Plugins.Contracts
{
    public class GetTotalAmount:IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            

            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory =
                        (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity )
            {
                try
                {
                    Entity entity = (Entity)context.InputParameters["Target"];

                    if (entity.LogicalName != "mb_contractline" && (context.MessageName != "Create" || context.MessageName != "Update"))
                        return;

                    tracingService.Trace(context.MessageName+" - "+context.Depth);

                    //if (context.Depth > 1)
                    //    return;

                    Entity PreImg = null;
                    if (context.PreEntityImages.Contains("PreImg"))
                    {
                        PreImg = (Entity)context.PreEntityImages["PreImg"];
                    }

                    tracingService.Trace("1");

                    if (!entity.Contains("mb_contract") && !PreImg.Contains("mb_contract"))
                        return;

                    tracingService.Trace("2");

                    EntityReference contract = null;

                    if(entity.Contains("mb_contract"))
                        contract = (EntityReference)entity["mb_contract"];

                    else if(PreImg.Contains("mb_contract")) 
                        contract = (EntityReference)PreImg["mb_contract"];


                    if (contract is null)
                        return;

                    
                    

                    // Define Condition Values
                    var query_mb_contract = contract.Id;
                    var query = new QueryExpression("mb_contractline");
                    query.ColumnSet.AddColumns("mb_totalamount");
                    query.Criteria.AddCondition("mb_contract", ConditionOperator.Equal, query_mb_contract);

                    EntityCollection ec = service.RetrieveMultiple(query);
                    decimal totalValue = 0;
                    foreach(var ent in ec.Entities)
                    {
                        //decimal value =  
                        totalValue = totalValue + ((ent.Contains("mb_totalamount")) ? ((Money)ent["mb_totalamount"]).Value : 0);
                    }

                    tracingService.Trace("Total "+ totalValue);

                    Entity contract_ent = new Entity("mb_contract");
                    contract_ent["mb_totalamount"] = new Money(totalValue);
                    contract_ent.Id = contract.Id;
                    service.Update(contract_ent);

                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => "+ex.Message);
                }
            }
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
            {
                try
                {
                    EntityReference entityref = (EntityReference)context.InputParameters["Target"];

                    if (entityref.LogicalName != "mb_contractline" && context.MessageName != "EntityReference")
                        return;

                    tracingService.Trace(context.MessageName + " - " + context.Depth);

                    var PreImg = (context.PreEntityImages != null && context.PreEntityImages.Contains("PreImg")) ? 
                        context.PreEntityImages["PreImg"] : null;

                    

                    EntityReference contract = null;
                    if (PreImg.Contains("mb_contract"))
                        contract = (EntityReference)PreImg["mb_contract"];



                    if (contract is null)
                        return;

                    tracingService.Trace("contract ref is not null ");

                    Money price = (PreImg.Contains("mb_totalamount") ? (Money)PreImg["mb_totalamount"] : new Money(0)); 
                    Entity contractent = service.Retrieve("mb_contract", contract.Id, new ColumnSet ("mb_totalamount") ); 
                    //decimal total = contractent.GetAttributeValue<Money>("mb_totalamount").Value; tracingService.Trace("13");
                    decimal total = (contractent.Contains("mb_totalamount") ? ((Money)contractent["mb_totalamount"]).Value : 0); 
                    total = total - price.Value; 
                    contractent["mb_totalamount"] = total > 0 ? new Money(total) : new Money(0); 
                    contract.Id = contract.Id; 
                    service.Update(contractent); 





                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => " + ex.Message);
                }
            }
        }
    }

    public static class ExtensionMethods
    {
        public static T GetAliasedAttributeValue<T>(this Entity entity, string attributeName)
        {
            if (entity == null)
                return default(T);

            AliasedValue fieldAliasValue = entity.GetAttributeValue<AliasedValue>(attributeName);

            if (fieldAliasValue == null)
                return default(T);

            if (fieldAliasValue.Value != null && fieldAliasValue.Value.GetType() == typeof(T))
            {
                return (T)fieldAliasValue.Value;
            }

            return default(T);
        }
    }
}
