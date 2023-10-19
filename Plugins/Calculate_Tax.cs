using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NGX.Plugins.CalculateTaxinQuote
{
    public class CalculateTax : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Calculate Tax Plugin Called");

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

                    if (entity.LogicalName != "quotedetail" && (context.MessageName != "Create" || context.MessageName != "Update"))
                        return;

                    tracingService.Trace(context.MessageName + " - " + context.Depth);

                    Entity PreImg = null;
                    if (context.PreEntityImages.Contains("PreImg"))
                    {
                        PreImg = (Entity)context.PreEntityImages["PreImg"];
                    }

                    tracingService.Trace("1");

                    if (!(entity.Contains("cdb_taxrate") && entity.Contains("quantity") && entity.Contains("priceperunit")))
                        return;


                    decimal quantity = (decimal)entity["quantity"];
                    Money price = (Money)entity["priceperunit"];
                    decimal total = quantity * price.Value;
                    decimal qdtaxrate = (decimal)entity["cdb_taxrate"];
                    decimal calculateTax = total * (qdtaxrate / 100);
                    tracingService.Trace("2");

                    entity["tax"] = calculateTax;
                    tracingService.Trace("Tax Rate: " + qdtaxrate  +" total: "+total+" tax: "+calculateTax);
                    service.Update(entity);



                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => " + ex.Message);
                }
            }
        }
    }
}
