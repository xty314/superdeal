<%@ WebService language="C#" class="CItem"%>

using System;
using System.IO;
using System.Web.Services;
using System.Xml.Serialization;

using System.Data;
using System.Data.SqlClient;
using System.Web.Mail;

[WebService(Namespace="http://eznz.com")]
public class CItem : System.Web.Services.WebService
{
	private SFunctions f = new SFunctions();
		
	[WebMethod]
	public DataSet GetItemDetail(string code)
	{
		if(!f.CheckSQLAttack(code))
			return null;
		string sc = " SELECT * FROM product_details WHERE code=" + code;
		return f.QuerySQL(sc);
	}

	[WebMethod]
	public string GetItemPhotoType(string code)
	{
		string fileName = Server.MapPath("/pi/" + code + ".jpg");
		if(File.Exists(fileName))
			return "jpg";

		fileName = Server.MapPath("/pi/" + code + ".gif");
		if(File.Exists(fileName))
			return "gif";

		return null;
	}

	[WebMethod]
	public byte[] GetItemPhotoData(string fileNameWithoutPath)
	{
		string fileName = Server.MapPath("/pi/" + fileNameWithoutPath);
		if(!File.Exists(fileName))
		{
			return null;
		}
		FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		long nFileLen = fs.Length;
		byte[] myData = new byte[nFileLen];
		fs.Read(myData, 0, (int)nFileLen);
		fs.Close();
		return myData;
	}
}

class SFunctions
{
	private const string m_sDataSource = ";data source=localhost;";
	private const string m_sSecurityString = "User id=eznz;Password=9seqxtf7;Integrated Security=false;";
	private const string m_emailAlertTo = "alert@eznz.com";
	private const string m_sCompanyName = "times";	//site identifer, used for cache, sql db name etc, highest priority

	private SqlConnection myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);

	public SFunctions()
	{
	}

	public DataSet QuerySQL(string sc)
	{
		DataSet ds = new DataSet();
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			int rows = myCommand.Fill(ds);
			if(rows > 0)
				return ds;
		}
		catch(Exception e) 
		{
//			ShowExp(sc, e);
		}
		return null;
	}

	public bool CheckSQLAttack(string str)
	{
		if(str == null || str == "")
			return true;

		string s = str.TrimStart(null);
		s = s.TrimEnd(null);

		bool bUpdate = (s.IndexOf("update") >= 0);
		bool bDelete = (s.IndexOf("delete") >= 0);
		bool bDrop = (s.IndexOf("drop") >= 0);
		bool bCreate = (s.IndexOf("create") >= 0);
		bool bSelect = (s.IndexOf("select") >= 0);
		bool bQuote = (s.IndexOf("'") >= 0);
		bool bSpace = (s.IndexOf(" ") >= 0);

		if(bUpdate || bDelete || bDrop || bCreate || bSelect || bQuote || bSpace)
		{
//			string ip = Request.ServerVariables["REMOTE_ADDR"]; //cache ip
//			string rip = ""; //real ip
//			if(Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
//				rip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
//			else
//				rip = ip;

			string sbody = "SQL Injection Attack In Service detected and blocked. <br>";
//			sbody += "ip : " + rip + "<br>";
//			sbody += "user : " + Session["name"] + "<br>";
//			sbody += "email : " + Session["email"] + "<br>";
//			sbody += "Account# : " + Session["login_card_id"] + "<br>";
//			sbody += "URI : " + Request.ServerVariables["URL"] + "<br>";
			sbody += "Parameter : " + str + "<br><br>";

			MailMessage msgMail = new MailMessage();
			
			msgMail.To = "alert@eznz.com";
			msgMail.From = "alert@eznz.com";
			msgMail.Subject = "Warning, SQL Injection Attack !";
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.Body = sbody;

			SmtpMail.Send(msgMail);
			return false;
		}
		return true;
	}
}
