<%@ WebService language="C#" class="CEZNZDownItem"%>

using System;
using System.IO;
using System.Web.Services;
using System.Xml.Serialization;

using System.Data;
using System.Data.SqlClient;
using System.Web.Mail;
using System.Web.Security;

[WebService(Namespace="http://eznz.com")]
public class CEZNZDownItem : System.Web.Services.WebService
{
	private const string m_sDataSource = ";data source=localhost;";
	private const string m_sSecurityString = "User id=eznz;Password=9seqxtf7;Integrated Security=false;";
	private const string m_emailAlertTo = "faraway3163@163.com";
	private const string m_sCompanyName = "supermarket_2more";	//site identifer, used for cache, sql db name etc, highest priority
	private SqlConnection myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
	DataSet ds = new DataSet();
		
	private bool CheckSQLAttack(string str)
	{
		if(str == null || str == "")
			return true;
		bool bUpdate = (str.IndexOf("update") >= 0);
		bool bDelete = (str.IndexOf("delete") >= 0);
		bool bDrop = (str.IndexOf("drop") >= 0);
		bool bCreate = (str.IndexOf("create") >= 0);
		bool bSelect = (str.IndexOf("select") >= 0);
		bool bQuote = (str.IndexOf("'") >= 0);
		bool bSpace = (str.IndexOf(" ") >= 0);
		if(bUpdate || bDelete || bDrop || bCreate || bSelect || bQuote || bSpace)
			return false;
		return true;
	}

	private void Trim(ref string s)
	{
		if(s == null)
			return;
		s = s.TrimStart(null);
		s = s.TrimEnd(null);
	}
	private int MyIntParse(string s)
	{
		Trim(ref s);
		if(s == null || s == "")
			return 0;
		return (int)MyDoubleParse(s);
	}
	private double MyDoubleParse(string s)
	{
		Trim(ref s);
		if(s == null || s == "")
			return 0;
		if(s.IndexOf("(")==0 && s.IndexOf(")") == s.Length-1)
		{
			s = s.Replace("(", "");
			s = s.Replace(")", "");
			s = "-" + s;
		}
		double d = 0;
		try
		{
			d = double.Parse(s);
		}
		catch(Exception e)
		{
		}
		return d;
	}
	private string EncodeQuote(string s) //double single quote for sql statements
	{
		if(s == null)
			return null;
		string ss = "";
		for(int i=0; i<s.Length; i++)
		{
			if(s[i] == '\'')
				ss += '\''; //double it for SQL query
			ss += s[i];
		}
		return ss;
	}
	
	private string ProcessVIPSales(string barcode, double dTotal)
	{
		return "0";
	}
	
	[WebMethod]
	public string UploadInvoice(string branch_id, DataSet dst)
	{
		string sc = "";
		string sRet = CheckAuth(""); //use uid to pass auth string, uid should be empty on first call
		if(sRet == "")	//not authorized
			return "auth failed";
		if(!CheckSQLAttack(branch_id))
			return "sql attack";
		
		if(dst == null)
			return "dst null";
		if(dst.Tables["inv"] == null)
			return "inv table null";
		DataRow dr = dst.Tables["inv"].Rows[0];
//		string invoice_number = dr["invoice_number"].ToString();
		string barcode = dr["barcode"].ToString();
		double dPrice = MyDoubleParse(dr["price"].ToString());
		double dTax = Math.Round(dPrice * 0.125, 4);
		double dTotal = dPrice + dTax;
//		string commit_date = dr["commit_date"].ToString();
		string payment_type = dr["payment_type"].ToString();
		string card_id = "0";
		if(barcode != "")
		{
			card_id = ProcessVIPSales(barcode, dTotal);
		}
		
		string order_id = "";
		string inv_id = "";
		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO invoice (branch, price, tax, total, payment_type, paid, amount_paid, commit_date) ";
		sc += " VALUES(" + branch_id + ", " + dPrice + ", " + dTax + ", " + dTotal + ", " + payment_type;
		sc += ", 1, " + dTotal + ", GETDATE()) ";
		sc += " SELECT IDENT_CURRENT('invoice') AS id ";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(ds, "inv_id") <= 0)
				return "inv_id failed";
			inv_id = ds.Tables["inv_id"].Rows[0]["id"].ToString();
		}
		catch(Exception e)
		{
			return "sql error, e=" + e.ToString() + ",sc=" + sc;
		}

		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO orders (branch, number, invoice_number, card_id) VALUES(" + branch_id + ", 0, " + inv_id + ", " + card_id + ") ";
		sc += " SELECT IDENT_CURRENT('orders') AS id ";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(ds, "order_id") <= 0)
				return "order_id failed";
			order_id = ds.Tables["order_id"].Rows[0]["id"].ToString();
		}
		catch(Exception e)
		{
			return "sql error, e=" + e.ToString() + ",sc=" + sc;
		}

		if(dst.Tables["sales"] == null)
			return "";

		sc = " UPDATE invoice SET invoice_number = id WHERE id = " + inv_id + " ";
		for(int i=0; i<dst.Tables["sales"].Rows.Count; i++)
		{
			dr = dst.Tables["sales"].Rows[i];
			string code = dr["code"].ToString();
			string qty = dr["quantity"].ToString();
			string price = dr["commit_price"].ToString();
			string supplier = "";
			string supplier_code = "";
			string supplier_price = "";
			string name = "";
			
			if(ds.Tables["item"] != null)
				ds.Tables["item"].Clear();
			string scc = " SELECT supplier, supplier_code, supplier_price, name FROM code_relations WHERE code = " + code;
			try
			{
				SqlDataAdapter myCommand1 = new SqlDataAdapter(scc, myConnection);
				if(myCommand1.Fill(ds, "item") <= 0)
					continue;
				supplier = ds.Tables["item"].Rows[0]["supplier"].ToString();
				supplier_code = ds.Tables["item"].Rows[0]["supplier_code"].ToString();
				supplier_price = ds.Tables["item"].Rows[0]["supplier_price"].ToString();
				name = ds.Tables["item"].Rows[0]["name"].ToString();
				
				sc += " INSERT INTO order_item (id, code, quantity, commit_price, item_name, supplier, supplier_code, supplier_price) ";
				sc += " VALUES (" + order_id + ", " + code + ", " + qty + ", " + price + ", '" + EncodeQuote(name) + "' ";
				sc += ", '" + EncodeQuote(supplier) + "', '" + EncodeQuote(supplier_code) + "', " + supplier_price + ") ";
				sc += " INSERT INTO sales (invoice_number, code, quantity, commit_price, name, supplier, supplier_code, supplier_price) ";
				sc += " VALUES (" + inv_id + ", " + code + ", " + qty + ", " + price + ", '" + EncodeQuote(name) + "' ";
				sc += ", '" + EncodeQuote(supplier) + "', '" + EncodeQuote(supplier_code) + "', " + supplier_price + ") ";
                sc += " UPDATE stock_qty SET qty = qty - " + qty + " WHERE code = " + code + " AND branch_id = " + branch_id + " ";
            }
			catch(Exception e)
			{
				return "sql error, e=" + e.ToString() + ",sc=" + scc;
			}
		}		
		try
		{
			SqlCommand myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			return "sql error, e=" + e.ToString() + ", sc=" + sc;
		}

		if(dst.Tables["payment"] == null)
			return "";

		for(int i=0; i<dst.Tables["payment"].Rows.Count; i++)
		{
			dr = dst.Tables["payment"].Rows[i];
			string vip_card_id = "0";
			string vip_barcode = dr["vip_barcode"].ToString();
			if(vip_barcode != "")
			{
				if(ds.Tables["card_id"] != null)
					ds.Tables["card_id"].Clear();
				sc = " SELECT id FROM card WHERE barcode = '" + EncodeQuote(vip_barcode) + "' ";
				try
				{
					SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
					if(myCommand1.Fill(ds, "card_id") > 0)
						vip_card_id = ds.Tables["card_id"].Rows[0]["id"].ToString();
				}
				catch(Exception e)
				{
					return "sql error, e=" + e.ToString() + ",sc=" + sc;
				}
			}

			//do transaction
			SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
			myCommand.CommandType = CommandType.StoredProcedure;

			myCommand.Parameters.Add("@shop_branch", SqlDbType.Int).Value = branch_id;
			myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = dr["amount_applied"].ToString();
			myCommand.Parameters.Add("@paid_by", SqlDbType.VarChar).Value = "";
			myCommand.Parameters.Add("@bank", SqlDbType.VarChar).Value = "";
			myCommand.Parameters.Add("@branch", SqlDbType.VarChar).Value = "";
			myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = "1116";
			myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = 0;
			myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = 0;
			myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = vip_card_id;
			myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = dr["payment_method"].ToString();
			myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = inv_id;
			myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = "";
			myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = "";
			myCommand.Parameters.Add("@finance", SqlDbType.Money).Value = "0";
			myCommand.Parameters.Add("@credit", SqlDbType.Money).Value = dr["credit"].ToString();
			myCommand.Parameters.Add("@bRefund", SqlDbType.Bit).Value = 0;
			myCommand.Parameters.Add("@amountList", SqlDbType.VarChar).Value = dr["amount_applied"].ToString();
			myCommand.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;
			try
			{
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch (Exception e)
			{
				myConnection.Close();
				return "sql error, e=" + e.ToString() + ", sc=" + sc;
			}
		}
		return "";
	}

	[WebMethod]
	public DataSet GetUpdatedItem(string branch_id, string uid) //delete update record if uid not empty, successfully updated
	{
		string sc = "";
		string sRet = CheckAuth(uid); //use uid to pass auth string, uid should be empty on first call
		if(sRet == "")	//not authorized
		{
			return null;
		}
		else if(sRet != "authorized")
		{
			sc = " SELECT '" + sRet + "' AS time_stamp ";
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				myCommand.Fill(ds, "auth");
			}
			catch(Exception e) 
			{
				return null;
			}
			return ds;
		}
		
		if(!CheckSQLAttack(branch_id))
			return null;
		int nItems = 0;
		if(MyIntParse(uid) > 0)
		{
			sc = " DELETE FROM updated_item WHERE id = " + uid;
			try
			{
				SqlCommand myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e) 
			{
				return null;
			}
		}
		
		sc = " SELECT TOP 1 u.id AS uid, c.id, c.code, c.name, c.name_cn, c.cat, c.s_cat, c.ss_cat, c.price1 ";
//		sc += " b.barcode, b.item_qty ";
		sc += " FROM code_relations c ";
		sc += " JOIN updated_item u ON u.item_code = c.code ";
//		sc += " LEFT OUTER JOIN barcode b ON b.item_code = c.code ";
		sc += " WHERE u.branch_id = " + branch_id;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "items") <= 0)
				return null;
		}
		catch(Exception e) 
		{
			return null;
		}
		
		string code = ds.Tables["items"].Rows[0]["code"].ToString();
		
		sc = " SELECT barcode, item_qty FROM barcode WHERE item_code = " + code + " AND barcode <> '' ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			nItems = myCommand.Fill(ds, "barcode");
		}
		catch(Exception e) 
		{
			return null;
		}
		return ds;
	}
	
	private string CheckAuth(string auth)
	{
		string ip = Context.Request.ServerVariables["REMOTE_ADDR"]; //cache ip
		string rip = ""; //real ip
		if(Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
			rip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
		else
			rip = ip;

		//remove expired auth key
		string sc = " DELETE FROM web_service_auth WHERE auth_end_time < GETDATE() ";	
		try
		{
			SqlCommand myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
		}
		
		string auth_id = "";
		sc = " SELECT id FROM web_service_auth WHERE ip = '" + rip + "' AND authorized = 1 ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "check_auth") > 0)
			{
				auth_id = ds.Tables["check_auth"].Rows[0]["id"].ToString();
				ds.Clear();
			}
		}
		catch(Exception e) 
		{
		}
		
		if(auth_id != "") //authorized
		{
			sc = " UPDATE web_service_auth SET auth_end_time = DATEADD(minute, 30, GETDATE()) WHERE id = " + auth_id; //another 30 minutes
			try
			{
				SqlCommand myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e) 
			{
			}
			return "authorized";
		}
		
		if(auth == "") //this is a start of sync
		{
			string sTimeStamp = DateTime.Now.ToOADate().ToString();
			string auth_key = "eznz_auth_" + sTimeStamp + "_darcy_0717";
			string password = FormsAuthentication.HashPasswordForStoringInConfigFile(auth_key, "md5");
			sc = " IF NOT EXISTS(SELECT id FROM web_service_auth WHERE ip = '" + rip + "') ";
			sc += " INSERT INTO web_service_auth (ip, password, auth_end_time) VALUES('" + ip + "', '" + password + "', DATEADD(minute, 10, GETDATE()) ) ";
			sc += " ELSE ";
			sc += " UPDATE web_service_auth SET password = '" + password + "', auth_end_time = DATEADD(minute, 10, GETDATE()) WHERE ip = '" + ip + "' ";
			try
			{
				SqlCommand myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e) 
			{
			}
			return sTimeStamp; //send back time stamp and waiting for password from client
		}
		else
		{
			auth_id = "";
			sc = " SELECT id FROM web_service_auth WHERE ip = '" + rip + "' AND password = '" + auth + "' ";
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				if(myCommand.Fill(ds, "check_auth") > 0)
				{
					auth_id = ds.Tables["check_auth"].Rows[0]["id"].ToString();
					ds.Clear();
				}
			}
			catch(Exception e) 
			{
			}
			if(auth_id != "")
			{
				sc = " UPDATE web_service_auth SET authorized = 1, auth_end_time = DATEADD(minute, 30, GETDATE()) WHERE id = " + auth_id; //another 30 minutes
				try
				{
					SqlCommand myCommand = new SqlCommand(sc);
					myCommand.Connection = myConnection;
					myCommand.Connection.Open();
					myCommand.ExecuteNonQuery();
					myCommand.Connection.Close();
				}
				catch(Exception e) 
				{
				}
				return "authorized";
			}
		}
		return "";
	}
}
