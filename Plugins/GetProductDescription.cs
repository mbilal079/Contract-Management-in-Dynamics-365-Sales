using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NGX.Plugins.Contracts
{
    public class GetProductDescription : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Product Description Plugin Called");

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

                    if (entity.LogicalName != "mb_contractline" && (context.MessageName != "Create" || context.MessageName != "Update"))
                        return;

                    tracingService.Trace(context.MessageName + " - " + context.Depth);

                    //if (context.Depth > 1)
                    //    return;

                    Entity PreImg = null;
                    if (context.PreEntityImages.Contains("PreImg"))
                    {
                        PreImg = (Entity)context.PreEntityImages["PreImg"];
                    }

                    

                    //entity["mb_totalamount"] = "0";
                    EntityReference product = null;
                    if (entity.Contains("mb_product"))
                        product = (EntityReference)entity["mb_product"];
                    //else if (PreImg.Contains("mb_product"))
                    //    product = (EntityReference)PreImg["mb_product"];
                    //tracingService.Trace("2");
                    if (product != null)
                    {
                        Entity prodent = service.Retrieve("product", product.Id, new ColumnSet("description"));

                        if (entity.Contains("mb_quoteproduct") && (bool)entity["mb_quoteproduct"])
                        {
                            
                        }
                        else if (prodent.Contains("description"))
                        {
                            entity["mb_description"] = prodent["description"];
                        }
                            
                        
                    }
                    decimal price = 0;
                    if (entity.Contains("mb_price"))
                        price = ((Money)entity["mb_price"]).Value;
                    else if(PreImg.Contains("mb_price"))
                        price = ((Money)PreImg["mb_price"]).Value;

                    //tracingService.Trace("price "+price);

                    int quantity = 0;
                    if (entity.Contains("mb_quantity"))
                        quantity = (int)entity["mb_quantity"];
                    else if (PreImg.Contains("mb_quantity"))
                        quantity = (int)PreImg["mb_quantity"];

                    //tracingService.Trace("quantity "+quantity);

                    entity["mb_totalamount"] = new Money (price * quantity);
                    //service.Update(entity);

                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => " + ex.Message);
                }
            }
        }
    }
}
