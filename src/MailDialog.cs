using System.Web.Mail;

namespace FSpot {
	public class MailDialog : GladeDialog ("mail_images") 
	{
	}

	public class MailImages
	{
		MailMessage message;

		public MailImages (IBrowsableCollection collection)
		{
			message = new MailMessage ();
			message.From = "lewing@gmail.com";
			message.To = "lewing@novell.com";
			message.Subject = "test";

			EsmtpMail mail = new EsmtpMail ("smtp.gmail.com", "lewing", "ricedream");
			mail.Send (message)
		}
	}
}
