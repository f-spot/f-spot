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
	public class SmugMugException : ApplicationException
	{
		public SmugMugException(string message) : base (message)
		{
			Log.Debug (message);
		}

		public SmugMugException (string message, Exception innerException) : base (message, innerException)
		{
			Log.Debug (message, innerException.ToString());
		}
	}
}
