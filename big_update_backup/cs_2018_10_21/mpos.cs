<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_access.cs" -->
<!-- #include file="sales_function.cs" -->
<!-- #include file="..\invoice.cs" -->

<script runat=server>

int m_line = 1; //line seed
bool m_bEOF = false;
string m_cardID = "0";
string m_level = "";

//for sales_functions
DataSet dst = new DataSet(); 
string m_invoiceNumber = "";

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	
	PrintAdminHeader();
	PrintAdminMenu();

	string cmd = Request.Form["cmd"];

	if(cmd == null)
	{
		Response.Write("<form action=? method=post>");
		Response.Write("<br><h3>Email Order Process <font color=red>(Ascent only for now)</font></h3>");
		Response.Write("<b>Paste email body here (copy every word, include signature)</b><br>");
		Response.Write("<textarea name=txt rows=30 cols=90>");
		Response.Write("</textarea><br>");
		Response.Write("<input type=submit name=cmd value='Process' " + Session["button_style"] + ">");
		Response.Write("</form>");
	}
	else if(cmd == "Process")
	{
		if(ProcessMailOrder())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx");
			Response.Write("\">");
		}
		return;
	}
}

bool ProcessMailOrder()
{
	PrepareNewSales();
	Session[m_sCompanyName + "_salestype"] = "mail_order";

	string s = Request.Form["txt"];
	if(s == null || s == "")
		return false;

	string spo = "our reference:";
	string scode = "your item code ";

	int i = 0; //for loop
	int p = 0; //char index
	string customer = "";
	string po_number = "";
	string shipto = "";

	string line = ReadLine(s);
//DEBUG("l=", line);
	int j = 0;
	while(!m_bEOF)
	{
		if(j++ > 1000)
			break; //protection
//DEBUG("line=", line);
		p = line.ToLower().IndexOf(spo);
		if(p >= 0)
		{
			po_number = line.Substring(spo.Length, line.Length - spo.Length);
			Trim(ref po_number);
			line = ReadLine(s);
			continue;
		}

		p = line.ToLower().IndexOf("please ship");
		if(p >= 0)
		{
			line = ReadLine(s);
//DEBUG("lin=", line);
			while(line == "")
			{
				if(j++ > 1000)
					break;
				line = ReadLine(s); //skip blank lines
//DEBUG("li=", line);
			}

			shipto += line + "\r\n";

			line = ReadLine(s);
//DEBUG("li=", line);
			//get all shipping address lines
			while(line != "")
			{
				if(j++ > 1000)
					break;
				shipto += line + "\r\n";
				line = ReadLine(s);
//DEBUG("l=", line);
			}
			line = ReadLine(s);
			continue;
		}

		p = line.ToLower().IndexOf("(phone");
		if(p >= 0)
		{
			shipto += line; //append phone number
			line = ReadLine(s);
			continue;
		}

		p = line.ToLower().IndexOf(" x ");
		if(p >= 0) //start qty
		{
			string sqty = line.Substring(0, p);
			int qty = MyIntParse(sqty);
			if(qty <= 0)
			{
				Response.Write("<font color=red>Error getting qty : string=" + sqty + "</font><br>");
				return false;
			}
//DEBUG("qty=", sqty);

			p = line.ToLower().IndexOf(scode);
			while(p < 0)
			{
				if(j++ > 1000)
					break;
				line = ReadLine(s);
				p = line.ToLower().IndexOf(scode);
			}
			string code = "";
			for(int m=p+scode.Length; m<line.Length; m++)
			{
				if(line[m] == ')')
					break;
				code += line[m];
			}
//DEBUG("code=", code);
			if(!AddToCart(code, sqty, "")) //let AddToCart get price
			{
				Response.Write("<b>AddToCart failed.</b><br>");
				return false;
			}
		}

		p = line.ToLower().IndexOf("ascent.co.nz");
		if(p >= 0)
		{
			customer = "Ascent Technology";
		}
		line = ReadLine(s);
	}

	//get customer

	if(customer != "")
	{
		SearchCardForCompany(customer);
	}

//DEBUG("po_number=", po_number);
//DEBUG("customer=", customer);
//DEBUG("card_id=", m_cardID);
//DEBUG("shipto=", shipto);
//	return false;

	if(shipto != "")
	{
		Session["sales_shipping_method"] = 4;
		Session["sales_special_shipto"] = "1";
		Session["sales_special_ship_to_addr"] = shipto;
	}
	Session["sales_customer_po_number"] = po_number;
	Session[m_sCompanyName + "_dealer_level_for_pos"] = m_level;
	Session["sales_customerid" + m_ssid] = m_cardID;
	return true;
}

string ReadLine(string s)
{
	int lines = 0;
	string sr = "";
	int p = 0;
	int j = 0;
	while(p < s.Length)
	{
		if(j++ > 100000)
			break;

		if(s[p] == '\r') //fine one return
		{
			lines++;
			if(lines == m_line) //target line number
			{
				m_line++;
				break;
			}
			if(p < sr.Length - 1 && s[p+1] == '\n') //skip new line mark
				p++;
			sr = "";			//start with new line			
		}
		else if(s[p] != '\n')
			sr += s[p];
		p++;
	}
	if(p >= s.Length)
		m_bEOF = true; //end of file

	return sr;
}

bool DecoderItemLine(string s)
{

	return true;
}

bool SearchCardForCompany(string company)
{
	string sc = " SELECT id, dealer_level FROM card WHERE company LIKE '%" + company + "%'";
	DataSet ds = new DataSet();
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds) == 1)
		{
			m_level = ds.Tables[0].Rows[0]["dealer_level"].ToString();
			m_cardID = ds.Tables[0].Rows[0]["id"].ToString();
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}
/*
Our Reference: AITC3-W1

Please ship the following item(s) to:

Matt Aitchison
Shine Tv, Level 7
164 Hereford St
Christchurch

(Phone: 0-3-366-3333)

1 x VIA EPIA-M10000 motherboard, VIA C3 1GHz Nehemiah Core, 266MHz FSB,
1xDIMM, DDR, Video, 1xPCI, 2xUS
     @ $269.08 + GST each    (your item code 10004) - $8.00 freight

1 x VIA Sereniti 2000, Mini ITX Case, 100W PSU, for EPIA-M Series
     @ $99.23 + GST each    (your item code 10006) - $0.00 freight


- Please confirm this order by email
- Direct ship to client - NO PAPERWORK
- Please advise by email when the items have shipped
- If the items cannot be shipped today, please advise when they are
expected to ship
- If our buy price or freight is different from that listed above,
please advise
- If an item code is specified and it is inconsistent with the item
described, please advise
- No goods are to be collected from your premises by our customers or
their agents unless we have made specific arrangements via email
beforehand
- All goods must be signed for unless we have made specific arrangements
via email beforehand
- All goods must be brand new, and not previously used or recycled
(ex-RA)
- Do not ship any phoned orders that we have not confirmed by email
- Do not ship any email orders that do not have a 6-character security
code here: NHW37L

NB: This email has been sent to: kathy@datawell.co.nz,
robert@datawell.co.nz

=================================================
Ascent Technology Ltd
Level 4, Eagle Technology House, 150-154 Willis St, Wellington PO Box
11-438, Manners St, Wellington Voice 0-4-802 3890  Fax 0-4-802 3891
Email josh@ascent.co.nz http://www.ascent.co.nz
=================================================

Regards
Josh
*/
</script>