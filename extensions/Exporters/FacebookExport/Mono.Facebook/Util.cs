//
// Mono.Facebook.Util.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class Util
	{
		private const string URL = "http://api.facebook.com/restserver.php?";
		private const string BOUNDARY = "SoMeTeXtWeWiLlNeVeRsEe";
		private const string LINE = "\r\n";

		private static Dictionary<int, XmlSerializer> serializer_dict = new Dictionary<int, XmlSerializer>();

		private FacebookParam VersionParam = FacebookParam.Create ("v", "1.0");
		private string api_key;
		private string secret;
		private bool use_json;

		private static XmlSerializer ErrorSerializer {
			get {
				return GetSerializer (typeof (Error));
			}
		}

		public Util (string api_key, string secret)
		{
			this.api_key = api_key;
			this.secret = secret;
		}

		public bool UseJson
		{
			get { return use_json; }
			set { use_json = value; }
		}

		internal string SharedSecret
		{
			get { return secret; }
			set { secret = value; }
		}

		internal string ApiKey
		{
			get { return api_key; }
		}

		public T GetResponse<T>(string method_name, params FacebookParam[] parameters)
		{
			string url = FormatGetUrl (method_name, parameters);
			byte[] response_bytes = GetResponseBytes (url);

			XmlSerializer response_serializer = GetSerializer (typeof (T));
			try {
				T response = (T)response_serializer.Deserialize(new MemoryStream(response_bytes));
				return response;
			}
			catch {
				Error error = (Error) ErrorSerializer.Deserialize (new MemoryStream (response_bytes));
				throw new FacebookException (error.ErrorCode, error.ErrorMsg);
			}
		}

		public XmlDocument GetResponse (string method_name, params FacebookParam[] parameters)
		{
			string url = FormatGetUrl (method_name, parameters);
			byte[] response_bytes = GetResponseBytes (url);

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (Encoding.Default.GetString (response_bytes));

			return doc;
		}

		internal Photo Upload (long aid, string caption, string path, string session_key)
		{
			// check for file
			FileInfo file = new FileInfo (path);

			if (!file.Exists)
				throw new FileNotFoundException ("Upload file not found", path);

			// create parameter string
			List<FacebookParam> parameter_list = new List<FacebookParam>();
			parameter_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));
			parameter_list.Add (FacebookParam.Create ("session_key", session_key));
			parameter_list.Add (FacebookParam.Create ("aid", aid));

			if (caption != null && caption != string.Empty)
				parameter_list.Add (FacebookParam.Create ("caption", caption));

			FacebookParam[] param_array = Sign ("facebook.photos.upload", parameter_list.ToArray ());
			string param_string = string.Empty;

			foreach (FacebookParam param in param_array) {
				param_string = param_string + "--" + BOUNDARY + LINE;
				param_string = param_string + "Content-Disposition: form-data; name=\"" + param.Name + "\"" + LINE + LINE + param.Value + LINE;
			}

			param_string = param_string + "--" + BOUNDARY + LINE
				+ "Content-Disposition: form-data; name=\"filename\"; filename=\"" + file.Name + "\"" + LINE
				+ "Content-Type: image/jpeg" + LINE + LINE;

			string closing_string = LINE + "--" + BOUNDARY + "--";

			// get bytes
			byte[] param_bytes = System.Text.Encoding.Default.GetBytes (param_string);
			byte[] closing_bytes = System.Text.Encoding.Default.GetBytes (closing_string);

			byte[] file_bytes = System.IO.File.ReadAllBytes (path);

			// compose
			List<byte> byte_list = new List<byte>(param_bytes.Length + file_bytes.Length + closing_bytes.Length);
			byte_list.AddRange (param_bytes);
			byte_list.AddRange (file_bytes);
			byte_list.AddRange (closing_bytes);

			byte[] final_bytes = new byte[byte_list.Count];
			byte_list.CopyTo (final_bytes);

			// create request
			WebClient wc = new WebClient ();
			wc.Headers.Set ("Content-Type", "multipart/form-data; boundary=" + BOUNDARY);
			wc.Headers.Add ("MIME-version", "1.0");

			// upload
			byte[] response = wc.UploadData ("http://api.facebook.com/restserver.php?", "POST", final_bytes);

			// deserialize
			XmlSerializer photo_serializer = new XmlSerializer (typeof (Photo));

			try {
				return (Photo)photo_serializer.Deserialize (new MemoryStream (response));
			}
			catch {
				Error error = (Error) ErrorSerializer.Deserialize (new MemoryStream (response));
				throw new FacebookException (error.ErrorCode, error.ErrorMsg);
			}
		}

		private static byte[] GetResponseBytes (string url)
		{
			WebRequest request = HttpWebRequest.Create (url);
			WebResponse response = null;

			try
			{
				response = request.GetResponse ();
				using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
				{
					return Encoding.UTF8.GetBytes(reader.ReadToEnd());
				}
			}
			finally
			{
				if (response != null)
					response.Close ();
			}
		}

		private string FormatGetUrl (string method_name, params FacebookParam[] parameters)
		{
			FacebookParam[] signed = Sign (method_name, parameters);

			StringBuilder builder = new StringBuilder (URL);

			for (int i = 0; i < signed.Length; i++)
			{
				if (i > 0)
					builder.Append ("&");

				builder.Append (signed[i].ToString ());
			}

			return builder.ToString ();
		}

		private static XmlSerializer GetSerializer (Type t)
		{
			int type_hash = t.GetHashCode ();

			if (!serializer_dict.ContainsKey (type_hash))
				serializer_dict.Add (type_hash, new XmlSerializer (t));

			return serializer_dict[type_hash];
		}

		internal FacebookParam[] Sign (string method_name, FacebookParam[] parameters)
		{
			List<FacebookParam> list = new List<FacebookParam> (parameters);
			list.Add (FacebookParam.Create ("method", method_name));
			list.Add (FacebookParam.Create ("api_key", api_key));
			list.Add (VersionParam);
			list.Sort ();

			StringBuilder values = new StringBuilder ();

			foreach (FacebookParam param in list)
				values.Append (param.ToString ());

			values.Append (secret);

			byte[] md5_result = MD5.Create ().ComputeHash (Encoding.ASCII.GetBytes (values.ToString ()));

			StringBuilder sig_builder = new StringBuilder ();

			foreach (byte b in md5_result)
				sig_builder.Append (b.ToString ("x2"));

			list.Add (FacebookParam.Create ("sig", sig_builder.ToString ()));

			return list.ToArray ();
		}

		internal static int GetIntFromString(string input)
		{
			try {
				return int.Parse(input);
			}
			catch {
				return 0;
			}
		}

		internal static bool GetBoolFromString(string input)
		{
			try {
				return bool.Parse(input);
			}
			catch {
				return false;
			}

		}
	}
}
