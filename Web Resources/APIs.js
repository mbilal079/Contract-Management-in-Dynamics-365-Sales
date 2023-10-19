

function getContract()
{
	console.log("Get Contract Called");
	Xrm.WebApi.retrieveRecord("mb_contract", contractID, "?$select=mb_name,mb_accountnumber,mb_account,mb_contractnumber,mb_startdate,mb_salesperson,mb_description").then(
	    function success(result)
	    {
	        console.log("Retrieved values: Name: " + result.mb_name);
	        // perform operations on record retrieval
	        getContractLines();
	    },
	    function (error)
	    {
	        console.log(error.message);
	        // handle error conditions
	    }
	);
}
function getContractLines()
{
	Xrm.WebApi.retrieveMultipleRecords("mb_contractline", "?$select=mb_name,mb_product,mb_productwritein,mb_quantity,mb_price,mb_totalamount&$filter=mb_contract/mb_contractid eq "+contractID).then(
	    function success(result) {
	        for (var i = 0; i < result.entities.length; i++) {
	            console.log("Contract Line = "+result.entities[i].mb_name);
	        }                    
	        // perform additional operations on retrieved records
	    },
	    function (error) {
	        console.log("Error in Contract Line = "+error.message);
	        // handle error conditions
	    }
	);
}
</script>