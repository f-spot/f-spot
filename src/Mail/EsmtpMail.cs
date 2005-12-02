//
// System.Web.Mail.SmtpMail.cs
//
// Author:
//    Lawrence Pit (loz@cable.a2000.nl)
//    Per Arneng (pt99par@student.bth.se) (SmtpMail.Send)
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web.Mail;
using System.Security.Permissions;

namespace FSpot.Mail
{
    	public class EsmtpMail
	{
		private string smtpServer = "localhost";
		private string username;
		private string password;
		private bool use_ssl;

		// Constructor		
		public EsmtpMail (string server, string username, string password, bool use_ssl)
		{
			this.password = password;
			this.smtpServer = server;
			this.username = username;
			this.use_ssl = use_ssl;
		}		

		// Properties
		public string SmtpServer {
			get { return smtpServer; } 
			set { smtpServer = value; }
		}

		public string Username {
			get { return username; }
			set { username = value; }
		}
		
		public string Password {
			get { return password; }
			set { password = value; }
		}
		
		public bool UseSsl {
			get { return use_ssl; }
			set { use_ssl = value; }
		}

		public void Send (MailMessage message) 
		{
			EsmtpClient smtp = new EsmtpClient (smtpServer, username, password, use_ssl);
			// wrap the MailMessage in a MailMessage wrapper for easier
			// access to properties and to add some functionality
			MailMessageWrapper messageWrapper = new MailMessageWrapper( message );
			
			smtp.Send (messageWrapper);
			
			smtp.Close ();
		}
		
		public void Send (string from, string to, string subject, string messageText) 
		{
			MailMessage message = new MailMessage ();
			message.From = from;
			message.To = to;
			message.Subject = subject;
			message.Body = messageText;
			Send (message);
		}
	}
}
