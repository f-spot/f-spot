//
// Mono.Google.Picasa.XmlUtil.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix (stephane@delcroix.org)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 S. Delcroix
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
// Check Picasa Web Albums Data Api at http://code.google.com/apis/picasaweb/gdata.html
//

using System;
using System.IO;
using System.Text;
using System.Xml;
namespace Mono.Google.Picasa {
	enum PicasaNamespaces {
		None,
		Atom,
		GPhoto,
		Photo,
		Media
	}

	internal class XmlUtil {
		static string [] URIs = new string [] {
			null,
			"http://www.w3.org/2005/Atom",
			"http://schemas.google.com/photos/2007",
			"http://www.pheed.com/pheed/",
			"http://search.yahoo.com/mrss/"
		};

		StringWriter sr;
		XmlTextWriter writer;

		public XmlUtil ()
		{
			StartDocument ();
		}

		public void StartDocument ()
		{
			sr = new StringWriter ();
			writer = new XmlTextWriter (sr);
			writer.Formatting = Formatting.Indented;
			writer.Indentation = 2;
			writer.Namespaces = true;
			writer.WriteStartElement (null, "entry", null);
			writer.WriteAttributeString ("xmlns", null, URIs [(int) PicasaNamespaces.Atom]);
			writer.WriteAttributeString ("xmlns", "media", null, URIs [(int) PicasaNamespaces.Media]);
			writer.WriteAttributeString ("xmlns", "gphoto", null, URIs [(int) PicasaNamespaces.GPhoto]);
		}

		public void WriteElementString (string name, string val)
		{
			writer.WriteElementString (name, val);
		}

		public void WriteElementString (string name, string val, string uri)
		{
			writer.WriteElementString (name, uri, val);
		}

		public void WriteElementString (string name, string val, PicasaNamespaces ns)
		{
			writer.WriteElementString (name, URIs [(int) ns], val);
		}

		public void WriteStartElement (string elem)
		{
			writer.WriteStartElement (elem, null);
		}

		public void WriteElementStringWithAttributes (string ename, string eval, params string[] attributes)
		{
			writer.WriteStartElement (null, ename, null);
			for (int i = 0; i < attributes.Length; i+=2)
				writer.WriteAttributeString (attributes[i], attributes[i+1]);
			writer.WriteRaw (eval);
			writer.WriteEndElement ();
		}

		public string GetDocumentString ()
		{
			writer.Close ();
			return sr.ToString ();
		}

		internal static void AddDefaultNamespaces (XmlNamespaceManager nsmgr) {
			nsmgr.AddNamespace ("atom", "http://www.w3.org/2005/Atom");
			nsmgr.AddNamespace ("photo", "http://www.pheed.com/pheed/");
			nsmgr.AddNamespace ("media", "http://search.yahoo.com/mrss/");
			nsmgr.AddNamespace ("gphoto", "http://schemas.google.com/photos/2007");
			nsmgr.AddNamespace ("batch", "http://schemas.google.com/gdata/batch");
		}
	}
}
