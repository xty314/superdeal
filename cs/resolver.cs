<%@Import Namespace="System.Net"%>
<script language=C# runat=server>
class CResolver
{
	private SqlConnection conn = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
	private DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

	//initialize
	public CResolver() 
	{
	}

	~CResolver() 
	{
	}

	public string Resolve(string ip)
	{
		string host = ip;
		bool bDoResolve = false;
		string sc = "SELECT host, logtime FROM dns_cache WHERE ip='" + ip + "'";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, conn);
			if(myCommand.Fill(ds, "host") > 0)
			{
				DateTime logtime = DateTime.Parse(ds.Tables["host"].Rows[0]["logtime"].ToString());
				if( (DateTime.Now - logtime).TotalDays > 7)
				{
					sc = "DELETE FROM dns_cache WHERE ip='" + ip + "'";
					bDoResolve = true;
					try
					{
						SqlCommand myComm = new SqlCommand(sc);
						myComm.Connection = conn;
						myComm.Connection.Open();
						myComm.ExecuteNonQuery();
						myComm.Connection.Close();
					}
					catch(Exception e) 
					{
						bDoResolve = false; //protection
					}
				}
				else
					host = ds.Tables["host"].Rows[0]["host"].ToString();
			}
			else
				bDoResolve = true;
		}
		catch(Exception e) 
		{
		}
		if(bDoResolve)
		{
			try
			{
				host = Dns.GetHostByAddress(ip).HostName;
			}
			catch(Exception e)
			{
				host = ip; //unresolvable
			}
			sc = "INSERT INTO dns_cache (ip, host) VALUES('" + ip + "', '" + host + "')";
			try
			{
				SqlCommand myCommand = new SqlCommand(sc);
				myCommand.Connection = conn;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e) 
			{
			}
		}
		return host;
	}
}
</script> 
