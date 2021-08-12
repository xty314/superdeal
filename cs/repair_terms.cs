<script runat=server>

//DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string terms = "";
void Page_Load(Object Src, EventArgs E ) 
{

	TS_PageLoad(); //do common things, LogVisit etc...
	terms = ReadSitePage("ra_term_condition");
	Response.Write(terms);

}

</script>