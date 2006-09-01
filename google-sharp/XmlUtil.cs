//
// Mono.Google.Picasa.XmlUtil.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Text;
using System.Xml;
namespace Mono.Google.Picasa {
	enum PicasaNamespaces {
		None,
		GPhoto,
		Photo,
		Media
	}

	class XmlUtil {
		static string [] URIs = new string [] {
			null,
			"http://picasaweb.google.com/lh/picasaweb/",
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
			writer.WriteProcessingInstruction ("xml", "version='1.0' encoding='utf-8'");
			writer.WriteStartElement (null, "rss", null);
			writer.WriteAttributeString ("version", "2.0");
			writer.WriteAttributeString ("xmlns", "gphoto", null, URIs [(int) PicasaNamespaces.GPhoto]);
			writer.WriteStartElement ("channel", null);
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

		public string GetDocumentString ()
		{
			writer.Close ();
			return sr.ToString ();
		}
	}
}

