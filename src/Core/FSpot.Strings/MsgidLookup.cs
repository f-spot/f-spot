// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace FSpot.Resources.Lang
{
	public static class MsgidLookup
	{
		static readonly Lazy<Dictionary<string, string>> EnglishToKey =
			new Lazy<Dictionary<string, string>> (BuildMap);

		static Dictionary<string, string> BuildMap ()
		{
			var map = new Dictionary<string, string> (StringComparer.Ordinal);
			var set = Strings.ResourceManager.GetResourceSet (CultureInfo.InvariantCulture, true, true);
			if (set == null)
				return map;

			foreach (DictionaryEntry entry in set) {
				if (entry.Key is string key && entry.Value is string value && value.Length > 0)
					map[value] = key;
			}

			return map;
		}

		public static string GetString (string msgid)
		{
			if (string.IsNullOrEmpty (msgid))
				return msgid;

			if (!EnglishToKey.Value.TryGetValue (msgid, out var key))
				return msgid;

			return Strings.ResourceManager.GetString (key, Strings.Culture) ?? msgid;
		}
	}
}
