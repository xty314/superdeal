<script runat=server>

bool AddStockQty(string code, int how_many)
{
	bool bExists = false;
	DataSet dsq = new DataSet();
	string sc = "SELECT qty FROM stock_qty WHERE code=" + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		bExists = (myAdapter.Fill(dsq, "qty") > 0);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(bExists)
	{
		int nStock_now = MyIntParse(dsq.Tables["qty"].Rows[0]["qty"].ToString());
		sc = "UPDATE stock_qty SET qty=qty+" how_many.ToString() + " WHERE code=" + code;
//unfinished here, DW 31.10.2002
	}
	return true;
}

</script>
