<%@Import Namespace="System.Net.Mail" %>
<%@Import Namespace="System.Net.Mime" %>
<%@Import Namespace="System.Net.Security" %>
<%@Import Namespace="System.Security.Cryptography.X509Certificates" %>

<script runat=server>

enum MailFormat {Text, Html};
static string sys_mail_from = "noreply@superdealnz.co.nz";
class MailAttachment
{
	public string m_fn = "";
	public MailAttachment(string fn)
	{
		m_fn = fn;
	}
}
class MailAttachments
{
	public int m_nFiles = 0;
	public string[] fn = new string[1024];
	public void Add(MailAttachment ma)
	{
		fn[m_nFiles++] = ma.m_fn;
	}
}
class MailMessage
{
	public string To = "";
	public string Cc = "";
	public string Bcc = "";
	public string From = "";
	public string Subject = "";
	public string Body = "";
	public MailFormat BodyFormat;
	public MailAttachments Attachments = new MailAttachments();
}
class SmtpMail
{
	public static bool Send(MailMessage mm)
	{
		string to = mm.To;
		string subject = mm.Subject;
		string body = mm.Body;
		string cc = mm.Cc;
		string bcc = mm.Bcc;
		string attachFilePath = "";
		for(int i=0; i<mm.Attachments.m_nFiles; i++)
		{
			if(i > 0)
				attachFilePath += ",";
			attachFilePath += mm.Attachments.fn[i];
		}
		
		if(to == "" || to.IndexOf("@") < 0)
		{
	//		ErrMsgAdmin("email to cannot be blank and must have @ symbol");
			return false;
		}
		
		string from = mm.From;//GetSiteSettings("smtp_mail_from", "noreply@farromail.co.nz");
		if(from == "")
			from = sys_mail_from;
		
		if(from == "" || from.IndexOf("@") < 0)
		{
	//		ErrMsgAdmin("Please config smtp_mail_from settings <a href=setting.aspx class=o>here</a>");
			return false;
		}	

		System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
	/*	client.Port = MyIntParse(port);
		client.Host = server;
		client.EnableSsl = bSsl;
		client.Timeout = 70000;
		client.DeliveryMethod = SmtpDeliveryMethod.Network;
		client.UseDefaultCredentials = false;
		client.Credentials = new System.Net.NetworkCredential(from, pwd);
	*/
		System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
		
		message.From = new System.Net.Mail.MailAddress(from);
		string[] aTo = to.Split(';');
		if(aTo.Length > 0)
		{
			for(int i=0; i<aTo.Length; i++)
			{
				message.To.Add(new System.Net.Mail.MailAddress(aTo[i]));
			}
		}
		
		if(cc != null && cc != "")
		{
			string[] cca = cc.Split(',');
			for(int i=0; i<cca.Length; i++)
			{
				if(cca[i].Trim() != "" && cca[i].IndexOf("@") > 0)
					message.CC.Add(new System.Net.Mail.MailAddress(cca[i]));
			}
		}
		if(bcc != null && bcc != "")
		{
			string[] bcca = bcc.Split(',');
			for(int i=0; i<bcca.Length; i++)
			{
				if(bcca[i].Trim() != "" && bcca[i].IndexOf("@") > 0)
					message.Bcc.Add(new System.Net.Mail.MailAddress(bcca[i]));
			}
		}
		message.Subject = subject;
		message.IsBodyHtml = true;
		message.BodyEncoding = UTF8Encoding.UTF8;
		message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
		message.Body = body;
		if(attachFilePath != "")
		{
			string[] sa = attachFilePath.Split(',');
			for(int i=0; i<sa.Length; i++)
			{
				string fn = sa[i];
				if(fn != "" && File.Exists(fn))
				{
					Attachment data = new System.Net.Mail.Attachment(fn, MediaTypeNames.Application.Octet);
					// Add time stamp information for the file.
					ContentDisposition disposition = data.ContentDisposition;
					disposition.CreationDate = System.IO.File.GetCreationTime(fn);
					disposition.ModificationDate = System.IO.File.GetLastWriteTime(fn);
					disposition.ReadDate = System.IO.File.GetLastAccessTime(fn);
					message.Attachments.Add(data);
				}
			}
		}
	/*	ServicePointManager.ServerCertificateValidationCallback =
		delegate(object s, X509Certificate certificate,
				 X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{ return true; };
	*/
		try
		{
			client.Send(message);
		}
		catch(Exception e)
		{
	//		ErrMsgAdmin("Send email failed. e=" + e.ToString());
	//		Response.End();
			return false;
		}
		return true;
	}
}
</script>
