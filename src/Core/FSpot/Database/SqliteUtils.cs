//
// SqliteUtils.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Hyena;

namespace FSpot.Database
{
	public static class SqliteUtils
	{
		public static object ToDbFormat (object value)
		{
			if (value == null)
				return null;

			return ToDbFormat (value.GetType (), value);
		}

		public static object ToDbFormat (Type type, object value)
		{
			if (type == typeof (string)) {
				// Treat blank strings or strings with only whitespace as null
				return value == null || string.IsNullOrEmpty (((string)value).Trim ())
					? null
					: value;
			} else if (type == typeof (DateTime)) {
				return DateTime.MinValue.Equals ((DateTime)value)
					? (object)null
					: DateTimeUtil.FromDateTime ((DateTime)value);
			} else if (type == typeof (TimeSpan)) {
				return TimeSpan.MinValue.Equals ((TimeSpan)value)
					? (object)null
					: ((TimeSpan)value).TotalMilliseconds;
			} else if (type == typeof (bool)) {
				return ((bool)value) ? 1 : 0;
			} else if (enum_types.Contains (type)) {
				return Convert.ChangeType (value, Enum.GetUnderlyingType (type));
			} else if (type.IsEnum) {
				enum_types.Add (type);
				return Convert.ChangeType (value, Enum.GetUnderlyingType (type));
			}

			return value;
		}

		public static T FromDbFormat<T> (object value)
		{
			object o = FromDbFormat (typeof (T), value);
			return o == null ? default : (T)o;
		}

		public static object FromDbFormat (Type type, object value)
		{
			if (Convert.IsDBNull (value))
				value = null;

			if (type == typeof (DateTime)) {
				if (value == null)
					return DateTime.MinValue;
				else if (!(value is long))
					value = Convert.ToInt64 (value);
				return DateTimeUtil.ToDateTime ((long)value);
			} else if (type == typeof (TimeSpan)) {
				if (value == null)
					return TimeSpan.MinValue;
				else if (!(value is long))
					value = Convert.ToInt64 (value);
				return TimeSpan.FromMilliseconds ((long)value);
			} else if (value == null) {
				if (type.IsValueType) {
					return Activator.CreateInstance (type);
				} else {
					return null;
				}
			} else if (type == typeof (bool)) {
				return ((long)value == 1);
			} else if (type == typeof (double?)) {
				if (value == null)
					return null;

				double double_value = ((float?)value).Value;
				return (double?)double_value;
			} else if (type.IsEnum) {
				return Enum.ToObject (type, value);
			} else {
				return Convert.ChangeType (value, type);
			}
		}

		internal static object NormalizeDbValue (object value)
		{
			if (value == null || value is DBNull)
				return null;
			if (value is int i)
				return (long)i;
			if (value is uint ui)
				return (long)ui;
			if (value is byte b)
				return (long)b;
			if (value is sbyte sb)
				return (long)sb;
			if (value is short s)
				return (long)s;
			if (value is ushort us)
				return (long)us;
			if (value is float f)
				return (double)f;
			return value;
		}

		internal static object[] NormalizeFunctionArgs (object[] args)
		{
			if (args == null)
				return Array.Empty<object> ();

			var normalized = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				normalized[i] = NormalizeDbValue (args[i]);
			}
			return normalized;
		}

		internal static object GetAs (object o, Type type)
		{
			if (o != null && o.GetType () == type)
				return o;

			if (o == null)
				o = null;
			else if (type == typeof (int))
				o = (int)(long)o;
			else if (type == typeof (uint))
				o = (uint)(long)o;
			else if (type == typeof (ulong))
				o = (ulong)(long)o;
			else if (type == typeof (float))
				o = (float)(double)o;

			if (o != null && o.GetType () == type)
				return o;

			return FromDbFormat (type, o);
		}

		static HashSet<Type> enum_types = new HashSet<Type> ();
	}
}
