<!-- #include file="..\cs\sqlstring.cs" -->
<%@Language=C# Debug="true" %>
<%@Import Namespace="System.Web.Caching" %>
<%@Import Namespace="System.Web.Mail" %>
<%@Import Namespace="System.Data" %>
<%@Import Namespace="System.Data.SqlClient" %>
<script runat=server language=c#>

protected void Page_Load(Object Src, EventArgs E ) 
{
	Response.Write("ASP.NET is running ok.<br>");


	SqlConnection myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
	SqlDataAdapter myAdapter;
	DataSet ds = new DataSet();
	
	string sc = " SELECT * FROM settings ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds);
		Response.Write("SQL connection is ok.<br>");
	}
	catch(Exception e) 
	{
		Response.Write("SQL connection is bad.<br>");
		Response.Write(e.ToString() + "<br><br>");
	}

	MailMessage msgMail = new MailMessage();
	msgMail.To = "darcy@eznz.com";
	msgMail.From = "darcy@eznz.com";
	msgMail.Subject = "test.aspx : " + Request.ServerVariables["SERVER_NAME"];
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "";

	try
	{
		SmtpMail.Send(msgMail);
		Response.Write("smtp is working.<br>");
	}
	catch(Exception e)
	{
		Response.Write("smtp is bad, sendmail failed.<br>");
		Response.Write(e.ToString() + "<br><br>");
	}


	Response.Write("<h5>Server VAriables : </h5>");
	foreach(string s in Request.ServerVariables.Keys) 
	{ 
	    Response.Write(s + " = " + Request.ServerVariables[s] + "<br>"); 
	}
}
</script>
