//
// QueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Gtk;

namespace Hyena.Query.Gui
{
	public abstract class QueryValueEntry : HBox
	{
		static Dictionary<Type, Type> types = new Dictionary<Type, Type> ();

		protected int DefaultWidth {
			get { return 170; }
		}

		public QueryValueEntry () : base ()
		{
			Spacing = 5;
		}

		public abstract QueryValue QueryValue { get; set; }

		public static QueryValueEntry Create (QueryValue qv)
		{
			Type qv_type = qv.GetType ();
			Type entry_type = null;

			foreach (KeyValuePair<Type, Type> pair in types) {
				if (pair.Value == qv_type) {
					entry_type = pair.Key;
					break;
				}
			}

			// If we don't have an entry type that's exactly for our type, take a more generic one
			if (entry_type == null) {
				foreach (KeyValuePair<Type, Type> pair in types) {
					if (qv_type.IsSubclassOf (pair.Value)) {
						entry_type = pair.Key;
						break;
					}
				}
			}

			if (entry_type != null) {
				var entry = Activator.CreateInstance (entry_type) as QueryValueEntry;
				entry.QueryValue = qv;
				return entry;
			}

			return null;
		}

		public static void AddSubType (Type entry_type, Type query_value_type)
		{
			types[entry_type] = query_value_type;
		}

		public static Type GetValueType (QueryValueEntry entry)
		{
			return types[entry.GetType ()];
		}

		static QueryValueEntry ()
		{
			AddSubType (typeof (StringQueryValueEntry), typeof (StringQueryValue));
			AddSubType (typeof (IntegerQueryValueEntry), typeof (IntegerQueryValue));
			AddSubType (typeof (DateQueryValueEntry), typeof (DateQueryValue));
			AddSubType (typeof (FileSizeQueryValueEntry), typeof (FileSizeQueryValue));
			AddSubType (typeof (TimeSpanQueryValueEntry), typeof (TimeSpanQueryValue));
			AddSubType (typeof (RelativeTimeSpanQueryValueEntry), typeof (RelativeTimeSpanQueryValue));
			AddSubType (typeof (NullQueryValueEntry), typeof (NullQueryValue));
		}
	}
}
