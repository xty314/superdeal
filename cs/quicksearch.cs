<script runat=server>

DataSet dst = new DataSet();

string m_supplier_code = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("manager"))
		return;

	if(Session["email"].ToString().IndexOf("eznz.com") >= 0)
		m_bEZNZAdmin = true;
		
    if(Request.QueryString ["s"] != "" && Request.QueryString["s"] != null)
	   m_supplier_code = Request.QueryString["s"].ToLower();

	if(!GetAllSettings())
		return;
	PrintList();
}

bool GetAllSettings()
{
	string Str =  m_supplier_code.Trim();
	double Num;
	bool isNum = double.TryParse(Str, out Num);
	int iCodeLength = m_supplier_code.Length;
//DEBUG("S " , iCodeLength.ToString());
	string sc = "SELECT c.id, c.name, c.name_cn, c.code, c.supplier_code, c.price1, c.special_price, c.is_special, ISNULL(c.promo_id,0) AS promo";
	sc += " FROM code_relations c  LEFT OUTER  JOIN barcode b ON b.supplier_code = c.supplier_code WHERE  ";
	sc += " Substring(c.supplier_code, 1, "+iCodeLength+") LIKE N'%"+m_supplier_code+"%' "; 
	if(!isNum)
	{
		sc += " OR LOWER(c.name) LIKE N'%" + m_supplier_code + "%'";
		sc += " OR LOWER(c.name_cn) LIKE N'%" + m_supplier_code + "%'";
	}
	sc += " OR LOWER(Substring(b.barcode, 1,"+ iCodeLength +")) = N'"+m_supplier_code +"'";
//DEBUG("sc " , sc);
//return false;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "search");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintList()
{
	Response.Write("<table cellspacing=0 cellpadding=0 width='100%'>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\" ><td>"+Lang("Code")+"</td><td>"+Lang("Discription")+"</td><td>"+Lang("Discrption Chinese")+"</td><td align=right>"+Lang("Selling Price")+"&nbsp;</td><td align=right>"+Lang("Specail Price")+"&nbsp;</td></tr>");

	string cat_old = "";
	bool bAlterColor = false;
	Response.Write("<tr>");
	for(int i=0; i<dst.Tables["search"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["search"].Rows[i];
		string id = dr["id"].ToString();
		string name_cn = dr["name_cn"].ToString();
		string name = dr["name"].ToString();
		string special_price = dr["special_price"].ToString();
		string price1 = dr["price1"].ToString();
		string is_special = dr["is_special"].ToString();
		string promo_id = dr["promo"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string code = dr["code"].ToString();
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a href='liveedit.aspx?code="+code+"' >"+id+"</a></td><td><a href='liveedit.aspx?code="+code+"'' >"+name+"</a></td><td><a href='liveedit.aspx?code="+code+"'>"+name_cn+"</a></td><td align=right>"+price1+"&nbsp;</td><td align=right>"+special_price+"&nbsp;</td></tr>");
   }
	
	
  if(dst.Tables["search"].Rows.Count <= 0)
  {
      Response.Write("<tr><td colspan =4 > Product Not Found</td></tr> ");
	  Response.Write("<tr><td colspan =4 ><a href=# onclick=\"javascript:history.go(-1)\">Back </a></td></tr> ");
   }	  
	  
	  Response.Write("</table>");
}

</script>