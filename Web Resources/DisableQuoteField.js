function disablefield(executionContext){
	var formContext = executionContext.getFormContext(); 
	var formtype = formContext.ui.getFormType()
	if(formtype == 1){
		formContext.getControl("mb_quoteproducts").setDisabled(true);
	}else{
		formContext.getControl("mb_quoteproducts").setDisabled(false);
	}
}