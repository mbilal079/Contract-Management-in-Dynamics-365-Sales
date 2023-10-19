using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace NGX.Plugins.Contracts
{
    public class GetProductsFromQuote:IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Get Products From Quote Plugin Called");

            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {
                    Entity entity = (Entity)context.InputParameters["Target"];
                    Entity PreImg = null;
                    if(context.PreEntityImages.Contains("PreImg"))
                        PreImg = (Entity)context.PreEntityImages["PreImg"];


                    IOrganizationServiceFactory serviceFactory =
                        (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    if (entity.LogicalName != "mb_contract" && (context.MessageName != "Create" || context.MessageName != "Update"))
                        return;

                    //Entity ent = new Entity("mb_contractline");
                    //ent["mb_name"] = "updated by plugin";
                    //ent["mb_price"] = new Money(100);
                    //ent["mb_contract"] = new EntityReference("mb_contract",entity.Id);
                    ////ent.Id = new Guid();
                    //service.Create(ent);

                    if (context.Depth > 1)
                        return;


                    if(context.MessageName == "Create")
                    {
                        //tracingService.Trace("in create");
                        string accountnumber = string.Empty;
                        if (entity.Contains("mb_account"))
                        {
                            EntityReference account = (EntityReference)entity["mb_account"];
                            Entity accent = service.Retrieve("account", account.Id, new ColumnSet("accountnumber"));
                            if (accent.Contains("accountnumber"))
                            {
                                accountnumber = (string)accent["accountnumber"];
                                entity["mb_accountnumber"] = accountnumber;
                                //tracingService.Trace("Account no: "+accountnumber);
                            }
                            
                        }
                        //string autonumber = contractAutoNumber(service);
                        int count = contractAutoNumber(service,accountnumber);
                        //tracingService.Trace("existing count "+count);
                        count = count + 1;
                        if (String.IsNullOrWhiteSpace(accountnumber))
                        {
                            entity["mb_contractnumber"] = count.ToString();
                        }
                        else
                        {
                            entity["mb_contractnumber"] = accountnumber + "-" + count;
                        }
                        
                        entity["mb_count"] = count;
                        //tracingService.Trace("number "+ entity["mb_contractnumber"]);
                    }


                    tracingService.Trace("Depth "+ context.Depth);
                    if (!entity.Contains("mb_quote"))
                        return;
                    //tracingService.Trace("1");
                    bool quoteproducts = false;
                    if (entity.Contains("mb_quoteproducts"))
                        quoteproducts = (bool)entity["mb_quoteproducts"];
                    else if(PreImg.Contains("mb_quoteproducts")) 
                        quoteproducts = (bool)PreImg["mb_quoteproducts"];

                    if (!quoteproducts)
                        return;

                    //tracingService.Trace("2");

                    EntityReference quote = null;

                    if (entity.Contains("mb_quote"))
                        quote = (EntityReference)entity["mb_quote"];

                    var query_quoteid = quote.Id;

                    //tracingService.Trace("3");
                    var query = new QueryExpression("quotedetail");

                    query.ColumnSet.AddColumns("priceperunit", "description", "productdescription", "quotedetailname", "productid", "quoteid","quantity","extendedamount");

                    query.Criteria.AddCondition("quoteid", ConditionOperator.Equal, query_quoteid);

                    EntityCollection ec = service.RetrieveMultiple(query);
                    //tracingService.Trace("4");
                    if(context.MessageName == "Update")
                    {
                        foreach (Entity products in ec.Entities)
                        {
                            Entity contractline = new Entity("mb_contractline");

                            contractline["mb_quoteproduct"] = true;
                            if (products.Contains("priceperunit"))
                                contractline["mb_price"] = new Money(((Money)products["priceperunit"]).Value);

                            if (products.Contains("quotedetailname"))
                                contractline["mb_name"] = products["quotedetailname"];

                            if (products.Contains("quantity"))
                                contractline["mb_quantity"] = Convert.ToInt32(products["quantity"]);

                            //contractline["mb_totalamount"] = products["extendedamount"]; tracingService.Trace("7");
                            contractline["mb_contract"] = new EntityReference("mb_contract", entity.Id);

                            if (products.Contains("productid"))
                                contractline["mb_product"] = new EntityReference("product", ((EntityReference)products["productid"]).Id);

                            if (products.Contains("productdescription"))
                            {
                                contractline["mb_productwritein"] = products["productdescription"];
                            }
                            if (products.Contains("description"))
                            {
                                contractline["mb_description"] = products["description"];
                                //tracingService.Trace("Description: " + products["description"]);
                            }

                            //contractline.Id = new Guid();
                            //tracingService.Trace("5");
                            service.Create(contractline);
                            //tracingService.Trace("6");
                        }
                    }
                    

                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Exception => " + ex);
                }
            }
        }
        public int contractAutoNumber(IOrganizationService service, string accountnumber)
        {
            string automnumber = "NGX-";
            //string lastnumber = "1000";
            string lastnumber = "1";
            int count = 0;
            var query = new QueryExpression("mb_contract");
            query.TopCount = 1;
            query.ColumnSet.AddColumns("mb_contractnumber","mb_count");
            if (!String.IsNullOrWhiteSpace(accountnumber))
            {
                query.Criteria.AddCondition("mb_accountnumber", ConditionOperator.Equal, accountnumber);
            }
            else
            {
                query.Criteria.AddCondition("mb_accountnumber", ConditionOperator.Null);
            }
            
            query.AddOrder("createdon", OrderType.Descending);
            EntityCollection ec = service.RetrieveMultiple(query);
            foreach(var en in ec.Entities)
            {
                //lastnumber = en.GetAttributeValue<string>("mb_contractnumber");
                count = en.GetAttributeValue<int>("mb_count");
                
            }
            if (lastnumber.Contains("-"))
            {
                string[] subs = lastnumber.Split('-');
                string prevnumber = lastnumber =  subs[1];
                lastnumber = (Convert.ToInt32(prevnumber)+1).ToString();
            }
            
            string random = randomString();
            automnumber +=  lastnumber +"-"+ random;
            return count;
        }
        public string randomString()
        {
            int length = 4;

            // creating a StringBuilder object()
            StringBuilder str_build = new StringBuilder();
            Random random = new Random();

            char letter;

            for (int i = 0; i < length; i++)
            {
                double flt = random.NextDouble();
                int shift = Convert.ToInt32(Math.Floor(25 * flt));
                letter = Convert.ToChar(shift + 65);
                str_build.Append(letter);
            }
            return str_build.ToString();
        }
    }
}
