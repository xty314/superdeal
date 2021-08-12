<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["id"] == null || Request.QueryString["id"] == "")
	{
		Response.Redirect("olist.aspx?unchecked=1");
		return;
	}
	
	string id = Request.QueryString["id"];
	string un_checked = "1";
	if(Request.QueryString["pass"] == "1")
		un_checked = "0";

	string sc = " UPDATE orders SET unchecked = " + un_checked + " WHERE id = " + id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	BackToLastPage();
}
</script>
