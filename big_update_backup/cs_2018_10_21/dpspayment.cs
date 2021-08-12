<!-- #include file="PxPay.cs" -->

<%@Import Namespace="System.Diagnostics" %>
<%@Import Namespace="System.Text" %>

<%@Import Namespace="System.Reflection" %>
<%@Import Namespace="System.Xml.Linq" %>
<%@Import Namespace="System.Web.UI.WebControls.WebParts" %>
<%@Import Namespace="System.Web.UI.WebControls" %>
<%@Import Namespace="System.Web.UI.HtmlControls" %>
<%@Import Namespace="System.Web.UI" %>
<%@Import Namespace="System.Web.Security" %>
<%@Import Namespace="System.Web" %>
<%@Import Namespace="System.Linq" %>
<%@Import Namespace="System.Configuration" %>
<%@Import Namespace="System" %>

<script runat=server>

	DataSet dst = new DataSet();
    protected void Page_Load(object sender, EventArgs e)
    {
		

        TS_PageLoad(); //do common things, LogVisit etc...
//myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
//		myConnection = new SqlConnection("Initial Catalog=browse;data source=localhost;User id=eznz;Password=9seqxtf7;Integrated Security=false;");
//        if( Request.Form["txtMerchantReference"] == null || Request.Form["txtMerchantReference"] == "")
//            return;
//        string order_number = Request.Form["txtMerchantReference"];
        if(Request.Form["cmd"] == " ")
        {
          /*  if(beProcessed(order_number))
		    {
				Response.Write("<meta http-equiv=\"refresh\" content=\"0;url=oops.aspx?\">");
				return;
			}
            beOnlineProcessed(order_number);*/
            GoToDPS();
        }
//			Button1_Click(sender, e);
		//Determine if the page request is for a user returning from the payment page
        string ResultQs = Request.QueryString["result"];
        if (!string.IsNullOrEmpty(ResultQs))
        {
            string PxPayUserId = ConfigurationManager.AppSettings["PxPayUserId"];
            string PxPayKey = ConfigurationManager.AppSettings["PxPayKey"];
            // Obtain the transaction result
            PxPay WS = new PxPay(PxPayUserId, PxPayKey);

            ResponseOutput output = WS.ProcessResponse(ResultQs);
			
            string sSuccess = "";
			string sType = "";
			string sCurrencyInput = "";
			string sCurrencySet = "";
			string sMerchantRef ="";
			string sTxt1 = "";
			string sTxt2 = "";
			string sTxt3 ="";
			string sAuthCode = "";
			string sCardType ="";
			string sCardHolderName ="";
			string sCardNumber ="";
			string sDateExpiry ="";
			string sDpsTxnRef = "";
			string sResponseTxt = "";
			string sDpsBillingId = "";
			string sClientInfo = "";
			string sTxnId = "";
			string sEmailAdd ="";
			string sBillingId = "";
			string sTxnMac = "";
			string sVolid = "";
			string sAmount = "";
		
			
			//string s

            // Write all the name value pairs out to a table

            Table t = new Table();
            TableRow tr;
            TableCell tc;
			


            PropertyInfo[] properties = output.GetType().GetProperties();

            foreach (PropertyInfo oPropertyInfo in properties)
            {

                if (oPropertyInfo.CanRead)
                {

                    tr = new TableRow();
		 		    tc = new TableCell();
                    tc.Text = oPropertyInfo.Name;
                    tr.Cells.Add(tc);
					
               		if(tc.Text == "valid")
						sVolid = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "AmountSettlement")
						sAmount = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "AuthCode")
						sAuthCode = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "CardName")
						sCardType = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "CardNumber")
						sCardNumber = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "DateExpiry")
						sDateExpiry = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "DpsTxnRef")
						sDpsTxnRef = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "Success")
						sSuccess = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "ResponseText")
						sResponseTxt = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "DpsBillingId")
						sDpsBillingId= (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "CardHolderName")
						sCardHolderName= (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "CurrencySettlement")
						sCurrencySet = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnData1")
						sTxt1 = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnData2")
						sTxt2  = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnData3")
						sTxt3 = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnType")
						sType = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "CurrencyInput")
						sCurrencyInput = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "MerchantReference")
						sMerchantRef = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "ClientInfo")
						sClientInfo = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnId")
						sTxnId = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "EmailAddress")
						sEmailAdd  = (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "BillingId")
						sBillingId= (string)oPropertyInfo.GetValue(output, null);
					if(tc.Text == "TxnMac")
						sTxnMac = (string)oPropertyInfo.GetValue(output, null);
					
                    tc = new TableCell();
                    tc.Text = (string)oPropertyInfo.GetValue(output, null);
                    tr.Cells.Add(tc);
					t.Rows.Add(tr);

                }
            }
		
           // if(sVolid == "1" && sSuccess == "1")
			//{
				
					
				string sc = " UPDATE orders SET paid ='" + sSuccess + "'";
                sc += ", cCardName = N'" + sCardHolderName + "'";
                sc += ", cCardType = '" + sCardType + "'";
                sc += ", cCardNum = '" + sCardNumber + "'";
                sc += ", cRefCode = '" + sDpsTxnRef + "'";
                sc += ", cSuccess = '" + sSuccess + "'";
                sc += ", cResponseTxt = '" + sResponseTxt + "'";
                sc += " WHERE id=" +sMerchantRef;

			    try
	            {
		            myCommand = new SqlCommand(sc);
		            myCommand.Connection = myConnection;
		            myCommand.Connection.Open();
		            myCommand.ExecuteNonQuery();
		            myCommand.Connection.Close();
	            }   
	            catch(Exception e1)
	            {
		            ShowExp(sc, e1);
		            return;
	            }
				//Send Mail To Manager
			string sbody ="Dear Manager, <br>";
				sbody += " We would like to inform you that the online credit card payment with order number:";
				sbody += sMerchantRef +" has been "+ sResponseTxt +". <br>";
				sbody += "Details Below <br>================<br>";
				sbody += "Order Number:" + sMerchantRef+ "<br>";
				sbody += "Total Amount:$" + sAmount +"<br>";
				sbody += "Card Type : " +sCardType+"<br>";
                sbody += "Card Number :" +sCardNumber+ "<br>";
				sbody += "Card Holder :" + sCardHolderName+"<br>";
				sbody += "Transation Reference: "+ sDpsTxnRef + "<br>";
				sbody += "===================<br>";
				sbody += "System Information <br>";
				sbody += "===================<br>";
				sbody += "This email sent when the customer completed the online payment and has "+ sResponseTxt +" - ";
				sbody += "Check order ? please click the link below<br>";
				sbody += "<a href='http://dev.eznz.com/dev/aio//admin/olist.aspx?kw="+HttpUtility.UrlEncode(sMerchantRef)+"&cmd="+HttpUtility.UrlEncode("Search+Order")+"'>";
				sbody += " Order: " +sMerchantRef + "</a><br><br>";
				
				sbody += "<br>************************************************************************************************************<br>";
				sbody += "<p>WARNING - This email and any attachments may be confidential. If received in error, please delete and inform us by return email.</p>";
				sbody += "<p>Because emails and attachments may be interfered with, may contain computer viruses or other defects and may not be successfully<br> ";
				sbody += "replicated on other systems, you must be cautious. We cannot guarantee that what you receive is what<br>";
				sbody += "we sent. If you have any doubts<br>";
				sbody += "about the authenticity of an email from <u>info@gpos.co.nz</u> , please contact us immediately.</p>";
				sbody += "<p>It is also important to check for viruses and defects before opening or using attachments. </p>";
				sbody += "<br>************************************************************************************************************<br>";

				MailMessage msgMail = new MailMessage();
				msgMail.To = "info@gpos.co.nz";//GetSiteSettings("account_manager_email", "alert@eznz.com");
				msgMail.From = "info@gpos.co.nz";
				msgMail.Subject = "Online Payment Has Been "+ sResponseTxt+" Order No:"+ sMerchantRef;
				msgMail.BodyFormat = MailFormat.Html;
				msgMail.Body = sbody;
				SmtpMail.Send(msgMail);
			//}
			
            string res = ReadSitePage("online_payment_resposne");
            res = res.Replace("@@amount", double.Parse(sAmount).ToString("c"));
            res = res.Replace("@@trans_ref", sDpsTxnRef);
            res = res.Replace("@@status", sResponseTxt);
            res = res.Replace("@@trans_no",  sMerchantRef);
            Response.Write(res);
    
            Response.Write("</boby>");
			
			
		

        }


    }
	void GoToDPS()
    {
        string PxPayUserId = ConfigurationManager.AppSettings["PxPayUserId"];
        string PxPayKey = ConfigurationManager.AppSettings["PxPayKey"];

        PxPay WS = new PxPay(PxPayUserId, PxPayKey);

        RequestInput input = new RequestInput();

        input.AmountInput = Request.Form["txtAmountInput"];//txtAmountInput.Text;
        input.CurrencyInput = Request.Form["txtCurrencyInput"];//txtCurrencyInput.Text;
        input.MerchantReference = Request.Form["txtMerchantReference"];//txtMerchantReference.Text;
		input.EmailAddress = Request.Form["txtEmailAddress"];//txtEmailAddress.Text;
        input.TxnType = Request.Form["ddlTxnType"];//ddlTxnType.Text;
        input.UrlFail = Request.Url.GetLeftPart(UriPartial.Path);
        input.UrlSuccess = Request.Url.GetLeftPart(UriPartial.Path);

        Guid orderId = Guid.NewGuid();
        input.TxnId = orderId.ToString().Substring(0, 16);
        RequestOutput output = WS.GenerateRequest(input);

        if (output.valid == "1")
        {
            // Redirect user to payment page

            Response.Redirect(output.Url);
           
        }
    }
   
	bool beProcessed(string id)
    {
	    if(id == "")
		    return true;
	    if(dst.Tables["processed"] != null)
		    dst.Tables["processed"].Clear();
	    string sc = " SELECT online_processed FROM orders WHERE id = '"+id+"'";
	    try
	    {
		    myAdapter = new SqlDataAdapter(sc, myConnection);
		    if(myAdapter.Fill(dst, "processed") <= 0)
			    return false;
	    }
	    catch(Exception ex)
	    {
		    ShowExp(sc, ex);
		        return false;
	    }
	    if(dst.Tables["processed"].Rows.Count == 1)
	    {
		    bool beProcessed = bool.Parse(dst.Tables["processed"].Rows[0]["online_processed"].ToString());
		    if(beProcessed)
			    return true;
		    else
			    return false;
	    }
	    return true;
    }
    bool beOnlineProcessed(string id)
    {
	    if(id == "")
		    return true;
	    if(dst.Tables["processed"] != null)
		    dst.Tables["processed"].Clear();
	    string sc = " UPDATE orders SET online_processed = '1'WHERE id = '"+id+"'";
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
		    ShowExp(sc, e);
		    return false;
	    }
	    return true;
    }

</script>