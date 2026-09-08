// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Xml;

namespace FSpot.Resources.Lang
{
	public static class UiXmlTranslator
	{
		public static string Translate (string xml, Func<string, string> getString)
		{
			if (string.IsNullOrEmpty (xml))
				return xml;

			var doc = new XmlDocument { PreserveWhitespace = true };
			doc.LoadXml (xml);
			var nodes = doc.SelectNodes ("//*[@translatable='yes']");
			if (nodes == null)
				return xml;

			foreach (XmlElement node in nodes) {
				var original = node.InnerText;
				if (string.IsNullOrEmpty (original))
					continue;
				var translated = getString (original);
				if (translated != original)
					node.InnerText = translated;
			}

			return doc.OuterXml;
		}
	}
}
