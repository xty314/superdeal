<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>

<script runat=server>

DataSet ds = new DataSet();

string m_fileName = "";
string m_tables = "";
string m_columns = "";
string m_sql = "";

StringBuilder sb = new StringBuilder();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	string strPath = "";
	if(Request.QueryString["t"] == "done")
	{
		string fn = Request.QueryString["f"];
		if(fn == null || fn == "")
		{
			Response.Write("<br><br><h3>No file name?</h3>");
			return;
		}
		strPath = Server.MapPath(fn);
		FileInfo f = new FileInfo(strPath);

		strPath += ".zip";
		Response.Write("<br><br><br><center><font size=+1>Done! </font><a href=" + fn + " title='" + strPath + ".zip' class=o><font size=+1>download archive file(" + f.Length/1000 + "k)</font></a>");
		Response.Write("<br><br>Use ftp or direct copy from server if file too big");
		Response.Write("<br><br>Ask your SQL administrator to run the zipped query file to restore data");
		Response.Write("<br><b>(for safety, do a full database backup first)</b>");
		Response.Write("<br><br>");
//		Response.Write("<h3><font color=red>Do not click \"Go Back\" button on your browser !!!</font></h3>");
//		Response.Write("<font size=+1><b>We strongly suggest you </b></font><br>");
//		Response.Write("<font size=+1><b><a href=close.htm class=o>click here to close this window</a></b></font><br>");
//		Response.Write("<font size=+1><b>after downloading.</b></font>");
		return;
	}
	else if(Request.Form["cmd"] == null)
	{
		Response.Write("<br><center><h3>Archive Database</h3>");

		Response.Write("<form action=archive.aspx method=post>");
		Response.Write("<table width=55%>");

		Response.Write("<tr><td>Archive all records before <b> 01/01/</b>");
		Response.Write("<select name=year>");

		int end = MyIntParse(DateTime.Now.ToString("yyyy"));
		for(int i=2000; i<=end; i++)
		{
			Response.Write("<option value=" + i + ">" + i.ToString() + "</option>");
		}
		Response.Write("</select>");
		Response.Write("</td></tr>");

		Response.Write("<tr><td>&nbsp;</td></tr>");

		Response.Write("<tr><td><h5><font color=red>");
		Response.Write("All records(except product & card) before selected year will be archived to a restorable file, ");
		Response.Write("then removed from current database to speed up the system. ");
		Response.Write("</font></h5></td></tr>");
		
		Response.Write("<tr><td><h5><font color=red>");
		Response.Write("Orders, invoices, serial numbers ");
		Response.Write("will not be able to access once archived, so we recommend you to choose at least 3 years till now.");
		Response.Write("</font></h5></td></tr>");

		Response.Write("<tr><td><h5><font color=red>");
		Response.Write("This process will take some time depends on the amount of data to be archived, we suggest you do not do ");
		Response.Write("this during busy working time.");
		Response.Write("</font></h5></td></tr>");

		Response.Write("<tr><td align=center><input type=submit name=cmd value=Archive class=b ");
		Response.Write(" onclick=\"return confirm('Important, Backup your database before archive!\\r\\n");
		Response.Write("And, DO NOT close your browser while processing.\\r\\n");
		Response.Write("This will take quite a long time depends on the amount or data.");
		Response.Write("');\"></td></tr>");
		Response.Write("</table></center>");

		Response.Flush();
		PrintAdminFooter();
		return;
	}

	string dy = Request.Form["year"];
	string dd = "01/01/" + dy;
	string[] table = new String[64];
	string[] sc = new String[64];
	string[] sd = new String[64];
	
	int ts = 0;
	table[ts] = "order_item";
	sd[ts] = " DELETE FROM order_item WHERE id IN(SELECT id FROM orders WHERE record_date < '" + dd + "') ";
	sc[ts++] = " SELECT i.* FROM order_item i JOIN orders o ON o.id = i.id WHERE o.record_date < '" + dd + "' ";
	
	table[ts] = "order_kit";
	sd[ts] = " DELETE FROM order_kit WHERE id IN(SELECT id FROM orders WHERE record_date < '" + dd + "') ";
	sc[ts++] = " SELECT k.* FROM order_kit k JOIN orders o ON o.id = k.id WHERE o.record_date < '" + dd + "' ";

	table[ts] = "orders";
	sd[ts] = " DELETE FROM orders WHERE record_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM orders WHERE record_date < '" + dd + "' ";

	table[ts] = "sales";
	sd[ts] = " DELETE FROM sales WHERE invoice_number IN(SELECT invoice_number FROM invoice WHERE commit_date < '" + dd + "') ";
	sc[ts++] = " SELECT s.* FROM sales s JOIN invoice i ON s.invoice_number = i.invoice_number WHERE i.commit_date < '" + dd + "' ";
	
	table[ts] = "sales_kit";
	sd[ts] = " DELETE FROM sales_kit WHERE invoice_number IN (SELECT invoice_number FROM invoice WHERE commit_date < '" + dd + "') ";
	sc[ts++] = " SELECT s.* FROM sales_kit s JOIN invoice i ON s.invoice_number = i.invoice_number WHERE i.commit_date < '" + dd + "' ";

	table[ts] = "invoice_freight";
	sd[ts] = " DELETE FROM invoice_freight WHERE invoice_number IN(SELECT invoice_number FROM invoice WHERE commit_date < '" + dd + "') ";
	sc[ts++] = " SELECT s.* FROM invoice_freight s JOIN invoice i ON s.invoice_number = i.invoice_number WHERE i.commit_date < '" + dd + "' ";

	table[ts] = "sales_serial";
	sd[ts] = " DELETE FROM sales_serial WHERE invoice_number IN (SELECT invoice_number FROM invoice WHERE commit_date < '" + dd + "') ";
	sc[ts++] = " SELECT s.* FROM sales_serial s JOIN invoice i ON s.invoice_number = i.invoice_number WHERE i.commit_date < '" + dd + "' ";

	table[ts] = "invoice";
	sd[ts] = " DELETE FROM invoice WHERE commit_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM invoice WHERE commit_date < '" + dd + "' ";

	table[ts] = "purchase_item";
	sd[ts] = " DELETE FROM purchase_item WHERE id IN(SELECT id FROM purchase wHERE date_invoiced < '" + dd + "') ";
	sc[ts++] = " SELECT i.* FROM purchase_item i JOIN purchase p ON i.id = p.id WHERE p.date_invoiced < '" + dd + "' ";

	table[ts] = "purchase";
	sd[ts] = " DELETE FROM purchase WHERE date_invoiced < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM purchase WHERE date_invoiced < '" + dd + "' ";

	table[ts] = "serial_trace";
	sd[ts] = " DELETE FROM serial_trace WHERE logtime < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM serial_trace WHERE logtime < '" + dd + "' ";

	table[ts] = "stock";
	sd[ts] = " DELETE FROM stock WHERE purchase_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM stock WHERE purchase_date < '" + dd + "' ";

	table[ts] = "price_history";
	sd[ts] = " DELETE FROM price_history WHERE price_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM price_history WHERE price_date < '" + dd + "' ";

	table[ts] = "sra_item";
	sd[ts] = " DELETE FROM sra_item WHERE sra_id IN(SELECT id FROM sra WHERE date_created < '" + dd + "') ";
	sc[ts++] = " SELECT * FROM sra_item i JOIN sra s ON i.sra_id = s.id WHERE s.date_created < '" + dd + "' ";

	table[ts] = "sra";
	sd[ts] = " DELETE FROM sra WHERE date_created < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM sra WHERE date_created < '" + dd + "' ";

	table[ts] = "trans";
	sd[ts] = " DELETE FROM trans WHERE id IN(SELECT id FROM tran_detail WHERE trans_date < '" + dd + "') ";
	sc[ts++] = " SELECT t.* FROM trans t JOIN tran_detail d ON t.id = d.id WHERE d.trans_date < '" + dd + "' ";
	
	table[ts] = "tran_invoice";
	sd[ts] = " DELETE FROM tran_invoice WHERE tran_id IN(SELECT id FROM tran_detail WHERE trans_date < '" + dd + "') ";
	sc[ts++] = " SELECT t.* FROM tran_invoice t JOIN tran_detail d ON t.tran_id = t.id WHERE d.trans_date < '" + dd + "' ";

	table[ts] = "tran_detail";
	sd[ts] = " DELETE FROM tran_detail WHERE trans_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM tran_detail WHERE trans_date < '" + dd + "' ";

	table[ts] = "tran_deposit_id";
	sd[ts] = " DELETE FROM tran_deposit_id WHERE id IN(SELECT id FROM tran_deposit WHERE deposit_date < '" + dd + "') ";
	sc[ts++] = " SELECT i.* FROM tran_deposit_id i JOIN tran_deposit d ON i.id = d.id WHERE d.deposit_date < '" + dd + "' ";

	table[ts] = "tran_deposit";
	sd[ts] = " DELETE FROM tran_deposit WHERE deposit_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM tran_deposit WHERE deposit_date < '" + dd + "' ";
	
	table[ts] = "rma_stock";
	sd[ts] = " DELETE FROM rma_stock WHERE stock_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM rma_stock WHERE stock_date < '" + dd + "' ";
	
	table[ts] = "ra_statement";
	sd[ts] = " DELETE FROM ra_statement WHERE ra_id IN(SELECT id FROM rma WHERE repair_date < '" + dd + "') ";
	sc[ts++] = " SELECT * FROM ra_statement s JOIN rma r ON s.ra_id = r.id WHERE r.repair_date < '" + dd + "' ";

	table[ts] = "ra_replaced";
	sd[ts] = " DELETE FROM ra_replaced WHERE ra_id IN(SELECT id FROM rma WHERE repair_date < '" + dd + "') ";
	sc[ts++] = " SELECT * FROM ra_replaced s JOIN rma r ON s.ra_id = r.id WHERE r.repair_date < '" + dd + "' ";

	table[ts] = "rma";
	sd[ts] = " DELETE FROM rma WHERE repair_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM rma WHERE repair_date < '" + dd + "' ";

	table[ts] = "repair";
	sd[ts] = " DELETE FROM repair WHERE repair_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM repair WHERE repair_date < '" + dd + "' ";

	table[ts] = "repair_log";
	sd[ts] = " DELETE FROM repair_log WHERE log_time < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM repair_log WHERE log_time < '" + dd + "' ";

	table[ts] = "return_sn";
	sd[ts] = " DELETE FROM return_sn WHERE replaced_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM return_sn WHERE replaced_date < '" + dd + "' ";

	table[ts] = "ra_freight";
	sd[ts] = " DELETE FROM ra_freight WHERE depatch_date < '" + dd + "' ";
	sc[ts++] = " SELECT * FROM ra_freight WHERE depatch_date < '" + dd + "' ";


	Response.Write("<font size=+1>Archiving records, please wait.....</font>");
	for(int i=0; i<ts; i++)
	{
		if(!DoArchive(table[i], sc[i]))
		{
			return;
		}
	}
	if(sb.ToString() == "")
	{
		Response.Write("<h3>No Record Found</h3>");
		return;
	}

	Response.Write("<br><font size=+1>Record archived, clean up table, please wait.....</font>");
	for(int i=0; i<ts; i++)
	{
		if(!DoRemoveRecord(table[i], sd[i]))
		{
			return;
		}
	}

	strPath = Server.MapPath(".") + "\\archive_" + dy + ".sql";

//	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	Encoding enc = Encoding.GetEncoding(54936);
	byte[] Buffer = enc.GetBytes(sb.ToString());

	string sout = Encoding.ASCII.GetString(Buffer, 0, Buffer.Length);
	Buffer = enc.GetBytes(sout);

	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();

	if(!ZipOneFile("archive_" + dy + ".sql", strPath, strPath + ".zip", 9, 2048000))
		return;

	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=archive.aspx?t=done&f=" + HttpUtility.UrlEncode("archive_" + dy + ".sql.zip") +" \">");
	return;
}

bool DoArchive(string table, string sc)
{
	if(sc == "")
		return false;

	Response.Write("<br>Archiving table <b>" + table + "</b> ..");
	Response.Flush();
	ds.Clear();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, table) <= 0)
		{
			Response.Write("<font color=red>no record found.</font>");
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(!BuildResultSQL(table))
	{
		Response.Write("sth wrong");
		return false;;
	}
	Response.Write("done.");
	Response.Flush();
	return true;
}

bool HasIdent(string table)
{
	string sc = " SET IDENTITY_INSERT " + table + " ON ";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		myCommand.Connection.Close();
		return false;
	}
	
	sc = " SET IDENTITY_INSERT " + table + " OFF ";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		myCommand.Connection.Close();
		ShowExp(sc, e);
		return true;
	}
	return true;
}

bool BuildResultSQL(string table)
{
	int i = 0;

	Response.Write(".");
	Response.Flush();
	DataColumnCollection dc = ds.Tables[table].Columns;

	string[] dtype = new string[1024];
	bool bHasIden = HasIdent(table);
	m_columns = "";
	for(i=0; i<dc.Count; i++)
	{
		m_columns += dc[i].ColumnName;
		if(i < dc.Count - 1)
			m_columns += ", ";
	}
	if(bHasIden)
		sb.Append("SET IDENTITY_INSERT " + table + " ON \r\n");
	
	DataRow dr = null;
	
	Type t = null;
	Type tString = System.Type.GetType("System.String");
	Type tBool = System.Type.GetType("System.Boolean");
	Type tDateTime = System.Type.GetType("System.DateTime");

	bool bQuote = false;
	for(i=0; i<ds.Tables[table].Rows.Count; i++)
	{
		Response.Write(".");
		Response.Flush();

		dr = ds.Tables[table].Rows[i];
		sb.Append("INSERT INTO " + table + "(" + m_columns + ") VALUES(");
		for(int j=0; j<ds.Tables[table].Columns.Count; j++)
		{
			string svalue = ds.Tables[table].Rows[i][j].ToString();
			bQuote = false;
			t = ds.Tables[table].Columns[j].DataType;
			if(t == tString || t == tDateTime)
				bQuote = true;
			else if(t == tBool)
			{
				if(bool.Parse(svalue))
					svalue = "1";
				else
					svalue = "0";
			}

			if(j > 0)
				sb.Append(",");
			if(bQuote)
				sb.Append("'");
			else if(svalue == "")
				svalue = "null";
			sb.Append(EncodeQuote(svalue));
			if(bQuote)
				sb.Append("'");
		}
		sb.Append(")\r\n");
	}
	
	if(bHasIden)
		sb.Append("SET IDENTITY_INSERT " + table + " OFF\r\n");
	return true;
}

bool DoRemoveRecord(string table, string sc)
{
	if(sc == "")
		return false;

	Response.Write("<br>Cleaning table <b>" + table + "</b> ...");
	Response.Flush();
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		myCommand.Connection.Close();
		return false;
	}
	Response.Write("done.");
	Response.Flush();
	return true;
}

bool ZipOneFile(string fileName, string FileToZip, string ZipedFile ,int CompressionLevel, int BlockSize)
{
	if(! System.IO.File.Exists(FileToZip)) 
	{
		throw new System.IO.FileNotFoundException("The specified file " + FileToZip + " could not be found. Zipping aborderd");
	}
	
	System.IO.FileStream StreamToZip = new System.IO.FileStream(FileToZip,System.IO.FileMode.Open , System.IO.FileAccess.Read);
	System.IO.FileStream ZipFile = System.IO.File.Create(ZipedFile);
	ZipOutputStream ZipStream = new ZipOutputStream(ZipFile);
	ZipEntry ZipEntry = new ZipEntry(fileName);
	ZipStream.PutNextEntry(ZipEntry);
	ZipStream.SetLevel(CompressionLevel);
	byte[] buffer = new byte[BlockSize];
	System.Int32 size =StreamToZip.Read(buffer,0,buffer.Length);
	ZipStream.Write(buffer,0,size);
	try {
		while (size < StreamToZip.Length) {
			int sizeRead =StreamToZip.Read(buffer,0,buffer.Length);
			ZipStream.Write(buffer,0,sizeRead);
			size += sizeRead;
		}
	} catch(System.Exception ex){
		throw ex;
	}
	ZipStream.Finish();
	ZipStream.Close();
	StreamToZip.Close();
	return true;
}

</script>
