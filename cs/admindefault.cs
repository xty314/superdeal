<!-- #include file="pnote.cs" -->
<!-- #include file="menu_link.cs" -->
<script runat=server>


void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	PrintAdminHeader();
	//if(!g_bPDA)
		PrintAdminMenu();
	GetQueryStrings();	
	
	/*if(g_bPDA)
	{
		string s = ReadSitePage("admin_default_pda");
		s = s.Replace("@@branch", PrintBranchOptions(""));
		Response.Write(s);
         
        Response.Redirect("pos_retail.aspx");
		return;
	}
*/
	
	bool m_bShowPic = false;
	m_bShowPic = MyBooleanParse(GetSiteSettings("Show_menu_image", "false", true));
	if(m_bShowPic)
	{
		if(Request.QueryString["ms"] != null && Request.QueryString["ms"] != "" && Request.QueryString["ms"] != "File")
				GetAllCatalog();
		else
		{
			if(g_bOrderOnlyVersion)
				Response.Write(ReadSitePage("admin_default_orderonly").Replace("@@companyTitle", m_sCompanyTitle));
			else
			{
			
				string s = ReadSitePage("admin_default");
			//	Response.Write(s.Replace("@@companyTitle", m_sCompanyTitle));
			/************************************ User Control Panel ************************************/
			  if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 2 )
		             s = ReadSitePage("branch_stocker");
              if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 3 )
				     s = ReadSitePage("tech");
			  if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 4 )
                     s = ReadSitePage("sales");
			  if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 5 )
                     s = ReadSitePage("branch_manager");
			  if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 6 )
                     s = ReadSitePage("warehouse_manager");
			  if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) == 7 )
                     s = ReadSitePage("marketing");
			   if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) >= 8)
                     s = ReadSitePage("admin_default");
			 
           
			    s = s.Replace("@@loginName", Session["name"].ToString());
			    Response.Write(s);
			/*	}else{
			string s = ReadSitePage("admin_default");
				s = s.Replace("@@loginName", Session["name"].ToString());
				Response.Write(s.Replace("@@companyTitle", m_sCompanyTitle));
				}*/
		   /************************************************************************************************/
				
			}
			if(!IsPostBack)
			{
			}
			//	GetAllCatalog();
			if(!g_bOrderOnlyVersion)
				ShowPublicNotice();
		}
	}
	else
	{
		string s = ReadSitePage("admin_default");
		if(g_bPDA)
		{
			s = ReadSitePage("admin_default_pda");
			s = s.Replace("@@branch", PrintBranchOptions(""));
		}

		if(!IsPostBack)
		{
		}

		if(!g_bOrderOnlyVersion)
			ShowPublicNotice();
	}
	LFooter.Text = m_sAdminFooter;
}

void GetQueryStrings()
{
}

</script>
<asp:Label id=LFooter runat=server/>
