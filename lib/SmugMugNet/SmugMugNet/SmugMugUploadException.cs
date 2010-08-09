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
	public sealed class SmugMugUploadException : ApplicationException
	{
		public SmugMugUploadException (string message, Exception innerException) : base (message, innerException)
		{
			Log.Debug (message, innerException.ToString ());
		}
	}
}
