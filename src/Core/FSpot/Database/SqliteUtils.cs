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

using Hyena;

namespace FSpot.Database
{
	public static class SqliteUtils
	{
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

	}
}
