// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

using FSpot.Resources.Lang;

using GtkBeans;

namespace FSpot.Gui
{
	public static class TranslatedBuilder
	{
		public static Builder FromResource (Assembly assembly, string resourceName)
		{
			if (assembly == null)
				assembly = Assembly.GetCallingAssembly ();

			var stream = assembly.GetManifestResourceStream (resourceName);
			if (stream == null)
				throw new ArgumentException ("Cannot get resource file '" + resourceName + "'", nameof (resourceName));

			using (stream)
			using (var reader = new StreamReader (stream)) {
				var xml = UiXmlTranslator.Translate (reader.ReadToEnd (), MsgidLookup.GetString);
				var builder = new Builder ();
				builder.AddFromString (xml);
				return builder;
			}
		}
	}
}
