<script runat=server>

DataSet dsschedule = new DataSet();

void CheckScheduleTasks()
{
	CheckDailyTasks();
}

void CheckDailyTasks()
{
	string last_date = GetSiteSettings("daily_task_current_date", DateTime.Now.AddDays(-1).ToString("dd-MM-YYYY"));
	string current = DateTime.Now.ToString("dd-MM-yyyy");
	if(last_date == current)
		return;

	SetSiteSettings("daily_task_current_date", current);
	CheckAutoPayment();
	CheckSpecialPrices();
}

bool CheckAutoPayment()
{
	dsschedule.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT * FROM auto_expense WHERE next_payment_date <= '" + DateTime.Now.ToString("dd-MM-yyyy") + "' ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsschedule, "ap") <= 0)
			return true;
	}
	catch(Exception e)
	{
		AlertAdmin("Check Autopayment failed", "sc=" + sc + "\r\n" + e.ToString());
		return false;
	}

	for(int i=0; i<dsschedule.Tables["ap"].Rows.Count; i++)
	{
		DataRow dr = dsschedule.Tables["ap"].Rows[i];
		string id = dr["id"].ToString(); //expense id
		string frequency = dr["frequency"].ToString();
		DoCopyExpense(id, frequency);
	}
	return true;
}

bool CheckSpecialPrices()
{
	dsschedule.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT code FROM code_relations WHERE special_price_end_date < '" + DateTime.Now.ToString("dd-MM-yyyy") + "' ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsschedule, "csp") <= 0)
			return true;
	}
	catch(Exception e)
	{
		AlertAdmin("Check Special Prices failed", "sc=" + sc + "\r\n" + e.ToString());
		return false;
	}

	sc = "";
	for(int i=0; i<dsschedule.Tables["csp"].Rows.Count; i++)
	{
		DataRow dr = dsschedule.Tables["csp"].Rows[i];
		string code = dr["code"].ToString(); //expense id
		sc += " UPDATE code_relations SET price1 = normal_price, special_price_end_date = NULL WHERE code = " + code;
		sc += " DELETE FROM specials WHERE code = " + code;
	}
//DEBUG("sc=", sc);
	if(sc != "")
	{
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
			AlertAdmin("End Special falied", ", sc=" + sc + "\r\n" + e.ToString());
			return false;
		}
	}
	return true;
}

bool DoCopyExpense(string id, string frequency)
{
	if(dsschedule.Tables["expense"] != null)
		dsschedule.Tables["expense"].Clear();

	string sc = " SELECT e.*, i.* ";
	sc += ", a.name4 + a.name1 AS expense_type ";
	sc += " FROM expense e ";
	sc += " JOIN expense_item i ON i.id=e.id ";
	sc += " JOIN account a ON a.id = e.to_account ";
	sc += " WHERE e.id = " + id;

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsschedule, "expense") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		AlertAdmin("Copy AutoPayment falied", "restore expense failed, sc=" + sc + "\r\n" + e.ToString());
		return false;
	}
	
	DataRow dr = dsschedule.Tables["expense"].Rows[0];
	string branch = dr["branch"].ToString();
	string customerID = dr["card_id"].ToString();
	string fromAccount = dr["from_account"].ToString();
	string toAccount = dr["to_account"].ToString();
	string paymentType = dr["payment_type"].ToString();
	string paymentRef = dr["payment_ref"].ToString();
	string paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
	string recorded_by = dr["recorded_by"].ToString();
	string note = dr["note"].ToString();
	string expense_type = dr["expense_type"].ToString();

	//get new id
	sc = " BEGIN TRANSACTION ";
	sc += " SET DATEFORMAT dmy ";
	sc += " INSERT INTO expense (card_id, from_account, to_account, payment_type ";
	sc += ", payment_ref, payment_date, recorded_by, note) ";
	sc += " VALUES(" + customerID;
	sc += ", " + fromAccount;
	sc += ", " + toAccount;
	sc += ", " + paymentType;
	sc += ", '" + EncodeQuote(paymentRef) + "' ";
	sc += ", '" + paymentDate + "' ";
	sc += ", '" + recorded_by + "' ";
	sc += ", 'Auto Payment' ";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('expense') AS id";
	sc += " COMMIT ";

	if(dsschedule.Tables["apid"] != null)
		dsschedule.Tables["apid"].Clear();

	string new_id = "";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dsschedule, "apid") == 1)
		{
			new_id = dsschedule.Tables["apid"].Rows[0]["id"].ToString();
		}
		else
		{
			AlertAdmin("Copy AutoPayment falied", "get new id failed, sc=" + sc + "\r\n");
			return false;
		}
	}
	catch(Exception e) 
	{
		AlertAdmin("Copy AutoPayment falied", "sc=" + sc + "\r\n" + e.ToString());
		return false;
	}

//	string invoice_date = DateTime.Parse(dr["invoice_date"].ToString()).ToString("dd-MM-yyyy");
	string invoice_date = DateTime.Now.ToString("dd-MM-yyyy"); //invoice_date used to calculate GST return, so no repeat
	double dSubTax = 0;
	double dSubTotal = 0;
	sc = " SET DATEFORMAT dmy ";
	for(int i=0; i<dsschedule.Tables["expense"].Rows.Count; i++)
	{
		dr = dsschedule.Tables["expense"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
//		string invoice_date = DateTime.Parse(dr["invoice_date"].ToString()).ToString("dd-MM-yyyy");
		string tax = dr["tax"].ToString();
		string total = dr["total"].ToString();
		double dTax = MyDoubleParse(tax);
		double dTotal = MyDoubleParse(total);

		dSubTax += dTax;
		dSubTotal += dTotal;
		
		sc += " INSERT INTO expense_item (id, invoice_number, invoice_date, tax, total) ";
		sc += " VALUES(" + new_id + ", '" + EncodeQuote(invoice_number) + "', '" + invoice_date + "' ";
		sc += ", " + tax + ", " + total + ") ";
	}

	sc += " UPDATE expense SET ";
	sc += " tax = " + dSubTax;
	sc += ", total = " + dSubTotal;
	sc += " WHERE id = " + new_id;

	//do transaction
	sc += " UPDATE account SET balance = balance - " + dSubTotal + " WHERE id = " + fromAccount;
	sc += " UPDATE account SET balance = balance + " + dSubTotal + " WHERE id = " + toAccount;

	//update next date
	DateTime d = DateTime.Now;
	int nf = MyIntParse(frequency);
	switch(nf)
	{
	case 1:
		d = d.AddDays(7);
		break;
	case 2:
		d = d.AddDays(14);
		break;
	case 3:
		d = d.AddDays(28);
		break;
	case 4:
		d = d.AddMonths(1);
		break;
	case 5:
		d = d.AddMonths(2);
		break;
	case 6:
		d = d.AddDays(84); //12 weeks
		break;
	case 7:
		d = d.AddMonths(3);
		break;
	case 8:
		d = d.AddMonths(6);
		break;
	case 9:
		d = d.AddYears(1);
		break;
	default:
		break;
	}

	sc += " UPDATE auto_expense SET next_payment_date = '" + d.ToString("dd-MM-yyyy") + "' WHERE id = " + id;
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
		AlertAdmin("Copy AutoPayment falied", "insert purchase_item failed, sc=" + sc + "\r\n" + e.ToString());
		return false;
	}

	string msg = "Auto payment commited:<br>\r\n";
	msg += "Expense ID : <a href=http://" + Request.ServerVariables["SERVER_NAME"] + "/admin/expense.aspx?id=" + new_id + ">" + id + ", click to view</a><br>\r\n";
	msg += "Expense Type : " + expense_type + "<br>\r\n";
	msg += "Total Amount : " + dSubTotal.ToString("c") + "<br>\r\n";
	msg += "Next Payment Date : " + d + "<br>\r\n";

	MailMessage msgMail = new MailMessage();
	
	msgMail.To = GetSiteSettings("manager_email", "alert@eznz.com");//m_emailAlertTo;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "Auto Payment Info";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = msg;

	SmtpMail.Send(msgMail);

	return true;
}
</script>
