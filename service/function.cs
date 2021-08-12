	class SFunctions
	{
		private const string m_sDataSource = ";data source=192.168.1.4;";
		private const string m_sSecurityString = "User id=eznz;Password=9seqxtf7;Integrated Security=false;";
		private const string m_emailAlertTo = "darcy@eznz.com";
		private const string m_sCompanyName = "demo";	//site identifer, used for cache, sql db name etc, highest priority

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

		private bool CheckSQLAttack(string str)
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
				return false;
/*
				string manager_email = GetSiteSettings("manager_email", "alert@eznz.com");
				string ip = Request.ServerVariables["REMOTE_ADDR"]; //cache ip
				string rip = ""; //real ip
				if(Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
					rip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
				else
					rip = ip;

				string sbody = "SQL Injection Attack detected and blocked. <br>";
				sbody += "ip : " + rip + "<br>";
				sbody += "user : " + Session["name"] + "<br>";
				sbody += "email : " + Session["email"] + "<br>";
				sbody += "Account# : " + Session["login_card_id"] + "<br>";
				sbody += "URI : " + Request.ServerVariables["URL"] + "<br>";
				sbody += "Parameter : " + str + "<br><br>";

				sbody += "This attack is potential, the attacker was trying take control of you database useing<br>";
				sbody += "a technic called 'SQL Injection Attack', which could issentially destory your database if succeeded.<br>";
				sbody += "<br>We strongly suggest that you investigate this accoun/person if account# or user name is showing.<br>";
				sbody += " Detailed log is available in database if evidence is needed to take legal action.<br>";

				sbody += "<br>EZNZ Team";

				MailMessage msgMail = new MailMessage();
				
				msgMail.To = manager_email;
				msgMail.Cc = "alert@eznz.com";
				msgMail.From = manager_email;
				msgMail.Subject = "Warning, SQL Injection Attack !";
				msgMail.BodyFormat = MailFormat.Html;
				msgMail.Body = sbody;

				SmtpMail.Send(msgMail);
				return false;
*/
			}
			return true;
		}
	}
