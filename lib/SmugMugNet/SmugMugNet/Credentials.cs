using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Specialized;
using Hyena;

namespace SmugMugNet
{
	public struct Credentials
	{
		public string session_id;
		public int user_id;
		public string password_hash;

		public string SessionID {
			get { return session_id; }
			set { session_id = value; }
		}

		public int UserID {
			get { return user_id; }
		}

		public string PasswordHash {
			get { return password_hash; }
		}

		public Credentials(string session_id, int user_id, string password_hash)
		{
			this.session_id = session_id;
			this.user_id = user_id;
			this.password_hash = password_hash;
		}
	}
}
