using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace UpdateTaxinQuote
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

                    

                    if (!(entity.Contains("cdb_taxrate") && entity.Contains("baseamount")))
                        return;

                    decimal qdtaxrate = (decimal)entity["cdb_taxrate"];
                    decimal amount = (decimal)entity["baseamount"];

                    tracingService.Trace("Tax Rate: "+ qdtaxrate+ " & Amount: "+amount);



                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => " + ex.Message);
                }
            }
        }
    }
    
}
