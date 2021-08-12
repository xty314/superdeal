<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_code = "";
string m_id = "";

void Page_Load(Object Src, EventArgs E ) 		
{
    TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
    m_code = MyIntParse(g("code")).ToString();
    if(m_code == "0")
    {
		ErrMsgAdmin("Invalid item code, please follow a proper link.");
		return;
    }
    m_id = MyIntParse(g("id")).ToString();
	m_type = g("t");
	m_action = g("a");
	m_cmd = p("cmd");
	switch(m_cmd)
	{
	case "Add Item Barcode":
	case "Add Carton Barcode":
	case "Add Box Barcode":
		if(DoAddRecord())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?code=" + m_code + "\">");
		break;
	case "Save":
		if(DoUpdateRecord())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?code=" + m_code + "\">");
		break;
	case "refresh":
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?code=" + m_code + "&t=" + m_type + "&a=" + m_action + "\">");
		break;
	}
	if(m_cmd != "")
		return; //if it's a form post then do nothing else, quit here
	if(m_action == "d") //delete
	{
		if(DoDeleteRecord())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?code=" + m_code + "\">"); //return to list
		return;	
	}	
	PrintAdminHeader();
	PrintAdminMenu();
	PrintMainForm();
}

bool PrintMainForm()
{
	string sc = "";
	sc += " SELECT c.name, b.* ";
	sc += " FROM code_relations c LEFT OUTER JOIN barcode b ON b.item_code = c.code ";
	sc += " WHERE c.code = " + m_code;
	sc += " ORDER BY b.item_qty, b.carton_qty, b.barcode ";
	int nRows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(ds, "barcode");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<br><center><h4>Edit Item Barcode</h4>");
	Response.Write("<form name=f action=?code=" + m_code + "&id=" + m_id + " method=post>");
	if(nRows <= 0)
	{
		ErrMsgAdmin("Item not found, code = " + m_code);
		return false;
	}
	
	//print item summary
	Response.Write("<h5>");
	DataRow dr = ds.Tables["barcode"].Rows[0];
	string spic = "../pi/" + m_code + ".jpg";
	if(File.Exists(Server.MapPath(spic)))
		Response.Write("<img height=48 src='" + spic + "'> &nbsp; ");
	Response.Write("Item " + m_code + " &nbsp; <font color=green>" + dr["name"].ToString() + "</font></h5>");
	
	Response.Write("<table width=55% cellspacing=0 cellpadding=0 border=1 class=t>");

	int i = 0;
	//print item barcodes first
	Response.Write("<tr><td colspan=4><b>Item Barcodes</b></td></tr>");
	Response.Write("<tr class=th>");
	Response.Write("<th>Barcode</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	for(; i<nRows; i++)
	{
		dr = ds.Tables["barcode"].Rows[i];
		string id = dr["id"].ToString();
		string barcode = dr["barcode"].ToString();
		double dItemQty = MyDoubleParse(dr["item_qty"].ToString());
//		double dCartonQty = MyDoubleParse(dr["carton_qty"].ToString());
//		if(dItemQty > 0)
//			break; //end of item barcodes
		if(barcode == "")
			continue; 

		Response.Write("<tr");
		if(i%2 != 0)
			Response.Write(" bgcolor=#EEEEEE"); //alter color
		Response.Write(">");
		if(m_action == "e" && id == m_id) //edit job
		{
			Response.Write("<td><input type=text name=barcode value='" + barcode + "' maxlength=250></td>");
			Response.Write("<td><input type=text name=item_qty value='" + dItemQty + "'></td>");
			Response.Write("<td>");
			Response.Write("<input type=submit name=cmd value='Save' class=b>");
			Response.Write("<input type=button value='Cancel' class=b onclick='history.go(-1);'>");
			Response.Write("</td>");
		}
		else
		{
			Response.Write("<td>" + barcode + "</td>");
			Response.Write("<td>" + dItemQty + "</td>");
			Response.Write("<td align=center>");
			Response.Write("<a href=?code=" + m_code + "&a=e&id=" + id + " class=o>Edit</a> ");
			Response.Write("<a href=?code=" + m_code + "&a=d&id=" + id + " class=o onclick=\"if(!window.confirm('Are you sure to delete?')){return false;}\">Del</a> ");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}
	if(m_action != "e")
	{
		Response.Write("<tr><td><input type=text name=barcode size=40 maxlength=250></td>");
		Response.Write("<td><input type=text name=item_qty></td>");
		Response.Write("<td align=right>");	
		Response.Write("<input type=submit name=cmd class=b value='Add Item Barcode'>");
		Response.Write("</td></tr>");
	}
/*	
	//print carton barcodes
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=4><b>Item Barcodes</b></td></tr>");
	Response.Write("<tr class=th>");
	Response.Write("<th>Barcode</th>");
	Response.Write("<th>ItemQty</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	
	for(; i<nRows; i++)
	{
		dr = ds.Tables["barcode"].Rows[i];
		string id = dr["id"].ToString();
		string barcode = dr["barcode"].ToString();
		double dItemQty = MyDoubleParse(dr["item_qty"].ToString());
		double dCartonQty = MyDoubleParse(dr["carton_qty"].ToString());
		if(dCartonQty > 0)
			break; //end of carton barcodes

		Response.Write("<tr");
		if(i%2 != 0)
			Response.Write(" bgcolor=#EEEEEE"); //alter color
		Response.Write(">");
		if(m_action == "e" && id == m_id) //edit job
		{
			Response.Write("<td><input type=text name=barcode value='" + barcode + "'></td>");
			Response.Write("<td><input type=text name=item_qty value='" + dItemQty + "'></td>");
			Response.Write("<td colspan=2 align=right>");
			Response.Write("<input type=submit name=cmd value='Save' class=b>");
			Response.Write("<input type=button value='Cancel' class=b onclick='history.go(-1);'>");
			Response.Write("</td>");
		}
		else
		{
			Response.Write("<td>" + barcode + "</td>");
			Response.Write("<td>" + dItemQty + "</td>");
			Response.Write("<td colspan=2 align=right>");
			Response.Write("<a href=?code=" + m_code + "&a=e&id=" + id + " class=o>Edit</a> ");
			Response.Write("<a href=?code=" + m_code + "&a=d&id=" + id + " class=o onclick=\"if(!window.confirm('Are you sure to delete?')){return false;}\">Del</a> ");
			Response.Write("</td>");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}
	Response.Write("<tr><td><input type=text name=barcode_carton size=40 maxlength=250></td>");
	Response.Write("<td colsapn=3 align=right><input type=text name=item_qty size=40 maxlength=250></td>");
	
		
	Response.Write("<tr><td colspan=4 align=right>");	
	Response.Write("<input type=submit name=cmd class=b value='Add Carton Barcode'>");
	Response.Write("</td></tr>");
*/		
	//print box barcodes
//    Response.Write("<tr><td>&nbsp;</td></tr>");
//    Response.Write("<tr><td colspan=4><b>Box Barcodes</b></td></tr>");
//    Response.Write("<tr class=th>");
//    Response.Write("<th>Barcode</th>");
//    Response.Write("<th>CartonBarcode</th>");
//    Response.Write("<th>CartonQty</th>");
//    Response.Write("</tr>");
//    for(; i<nRows; i++)
//    {
//        dr = ds.Tables["barcode"].Rows[i];
//        string id = dr["id"].ToString();
//        string barcode = dr["barcode"].ToString();
//        string carton_barcode = dr["carton_barcode"].ToString();
//        double dCartonQty = MyDoubleParse(dr["carton_qty"].ToString());

//        Response.Write("<tr");
//        if(i%2 != 0)
//            Response.Write(" bgcolor=#EEEEEE"); //alter color
//        Response.Write(">");
//        if(m_action == "e" && id == m_id) //edit job
//        {
//            Response.Write("<td><input type=text name=barcode value='" + barcode + "'></td>");
//            Response.Write("<td><input type=text name=carton_barcode value='" + carton_barcode + "'></td>");
//            Response.Write("<td><input type=text name=carton_qty value='" + dCartonQty + "'></td>");
//            Response.Write("<td>");
//            Response.Write("<input type=submit name=cmd value='Save' class=b>");
//            Response.Write("<input type=button value='Cancel' class=b onclick='history.go(-1);'>");
//            Response.Write("</td>");
//        }
//        else
//        {
//            Response.Write("<td>" + barcode + "</td>");
//            Response.Write("<td>" + carton_barcode + "</td>");
//            Response.Write("<td>" + dCartonQty + "</td>");
//            Response.Write("<td align=right>");
//            Response.Write("<a href=?code=" + m_code + "&a=e&id=" + id + " class=o>Edit</a> ");
//            Response.Write("<a href=?code=" + m_code + "&a=d&id=" + id + " class=o onclick=\"if(!window.confirm('Are you sure to delete?')){return false;}\">Del</a> ");
//            Response.Write("</td>");
//            Response.Write("</td>");
//        }
//        Response.Write("</tr>");
	//}
//    Response.Write("<tr><td><input type=text name=barcode_box size=40 maxlength=250></td>");
//    Response.Write("<td colsapn=3 align=right><input type=text name=carton_barcode size=40 maxlength=250></td>");	
//    Response.Write("<td colsapn=0 align=right><input type=text name=carton_qty size=20 maxlength=60></td>");
//    Response.Write("<tr><td colspan=4 align=right>");	
//    Response.Write("<input type=submit name=cmd class=b value='Add Box Barcode'>");
//    Response.Write("</td></tr>");
	Response.Write("</table></form>");
	Response.Write("<br><center><input type=button class=b value='Back to Edit Product' onclick=\"window.location='liveedit.aspx?code=" + m_code + "';\">");
    return true;
}

bool DoAddRecord()
{   
	string barcode = p("barcode");
	string item_qty = MyDoubleParse(p("item_qty")).ToString();
	
	string sc = " IF NOT EXISTS (SELECT id FROM barcode WHERE barcode = '" + EncodeQuote(barcode) + "') ";
	sc += " BEGIN ";
	sc += " INSERT INTO barcode (item_code, barcode, item_qty) ";
	sc += " VALUES ";
	sc += "( " + m_code;
	sc += ", '" + EncodeQuote(barcode) + "' ";
	sc += ", '" + item_qty + "'";
	sc += ") "; 
	sc += " END ";
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
		return false;
	}
	return true;
}

bool DoUpdateRecord()
{   string desc = p("desc");
	string barcode = p("barcode");
	if(barcode == "")
	{
		ErrMsgAdmin("barcode cannot be blank");
		return false;
	}
	string item_qty = MyDoubleParse(p("item_qty")).ToString();
	string carton_qty = MyDoubleParse(p("carton_qty")).ToString();
	string CartonBarcode=MyDoubleParse(p("CartonBarcode")).ToString();
	string CartonQty=MyDoubleParse(p("CartonQty")).ToString();
	
	string sc = " ";
	sc += " BEGIN ";
	sc += " UPDATE  barcode SET item_code = '" + m_code + "' ";
	sc += ", barcode = '" + EncodeQuote(barcode) + "' ";
	sc += ", item_qty = '" + item_qty + "' ";
//	sc += ", carton_qty = '" + carton_qty +"'";
//	sc += ", carton_barcode = '" + EncodeQuote(CartonBarcode) +"'";
//	sc += ", box_qty = '" + CartonQty +"'";
	sc += " WHERE id = '" +  m_id + "' ";
	sc += " END ";
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
		
		return false;
	}
	return true;
}

bool DoDeleteRecord()
{   string desc = p("desc");
	string barcode = p("barcode");
	string CartonBarcode=MyDoubleParse(p("CartonBarcode")).ToString();
	string CartonQty=MyDoubleParse(p("CartonQty")).ToString();
	//if(barcode == "")
	//{
	//    ErrMsgAdmin("barcode cannot be blank");
	//    return false;
	//}
	
	string item_qty = MyDoubleParse(p("item_qty")).ToString();
	string carton_qty = MyDoubleParse(p("carton_qty")).ToString();
	
	
	//string sc = " IF NOT EXISTS (SELECT id FROM barcode WHERE barcode = '" + EncodeQuote(barcode) + "') ";
	//sc += " BEGIN ";
	string sc="BEGIN";
	sc += " DELETE FROM barcode WHERE id = '" + m_id + "'";
	sc += " END ";
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
		
		return false;
	}
	return true;
}
</script>
