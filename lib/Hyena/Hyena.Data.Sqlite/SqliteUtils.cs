//
// SqliteUtils.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace Hyena.Data.Sqlite
{
    public static class SqliteUtils
    {
        internal static string GetType (Type type)
        {
            if (type == typeof (string)) {
                return "TEXT";
            } else if (type == typeof (int) || type == typeof (long) || type == typeof (bool)
                || type == typeof (DateTime) || type == typeof (TimeSpan) || type.IsEnum) {
                return "INTEGER";
            } else if (type == typeof (int?) || type == typeof (long?) || type == typeof (bool?)
                || type == typeof (uint?)) {
                return "INTEGER NULL";
            } else if (type == typeof (byte[])) {
                return "BLOB";
            } else if (type == typeof (double)) {
                return "REAL";
             } else if (type == typeof (double?)) {
                return "REAL NULL";
            } else {
                throw new Exception (String.Format (
                    "The type {0} cannot be bound to a database column.", type.Name));
            }
        }

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
                return value == null || String.IsNullOrEmpty (((string)value).Trim ())
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
            object o = FromDbFormat (typeof(T), value);
            return o == null ? default(T) : (T)o;
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

                double double_value = ((Single?) value).Value;
                return (double?) double_value;
            } else if (type.IsEnum) {
                return Enum.ToObject (type, value);
            } else {
                return Convert.ChangeType (value, type);
            }
        }

        internal static string BuildColumnSchema (string type, string name, string default_value,
            DatabaseColumnConstraints constraints)
        {
            StringBuilder builder = new StringBuilder ();
            builder.Append (name);
            builder.Append (' ');
            builder.Append (type);
            if ((constraints & DatabaseColumnConstraints.NotNull) > 0) {
                builder.Append (" NOT NULL");
            }
            if ((constraints & DatabaseColumnConstraints.Unique) > 0) {
                builder.Append (" UNIQUE");
            }
            if ((constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                builder.Append (" PRIMARY KEY");
            }
            if (default_value != null) {
                builder.Append (" DEFAULT ");
                builder.Append (default_value);
            }
            return builder.ToString ();
        }

        static HashSet<Type> enum_types = new HashSet<Type> ();
    }

    [SqliteFunction (Name = "HYENA_BINARY_FUNCTION", FuncType = FunctionType.Scalar, Arguments = 3)]
    public sealed class BinaryFunction : SqliteFunction
    {
        private static Dictionary<string, Func<object, object, object>> funcs = new Dictionary<string, Func<object, object, object>> ();

        public static void Add (string functionId, Func<object, object, object> func)
        {
            lock (funcs) {
                if (funcs.ContainsKey (functionId)) {
                    throw new ArgumentException (String.Format ("{0} is already taken", functionId), "functionId");
                }

                funcs[functionId] = func;
            }
        }

        public static void Remove (string functionId)
        {
            lock (funcs) {
                if (!funcs.ContainsKey (functionId)) {
                    throw new ArgumentException (String.Format ("{0} does not exist", functionId), "functionId");
                }

                funcs.Remove (functionId);
            }
        }

        public override object Invoke (object[] args)
        {
            Func<object, object, object> func;
            if (!funcs.TryGetValue (args[0] as string, out func))
                throw new ArgumentException (args[0] as string, "HYENA_BINARY_FUNCTION name (arg 0)");

            return func (args[1], args[2]);
        }
    }

    [SqliteFunction (Name = "HYENA_COLLATION_KEY", FuncType = FunctionType.Scalar, Arguments = 1)]
    internal class CollationKeyFunction : SqliteFunction
    {
        public override object Invoke (object[] args)
        {
            return Hyena.StringUtil.SortKey (args[0] as string);
        }
    }

    [SqliteFunction (Name = "HYENA_SEARCH_KEY", FuncType = FunctionType.Scalar, Arguments = 1)]
    internal class SearchKeyFunction : SqliteFunction
    {
        public override object Invoke (object[] args)
        {
            return Hyena.StringUtil.SearchKey (args[0] as string);
        }
    }

    [SqliteFunction (Name = "HYENA_MD5", FuncType = FunctionType.Scalar, Arguments = -1)]
    internal class Md5Function : SqliteFunction
    {
        public override object Invoke (object[] args)
        {
            int n_args = (int)(long) args[0];
            var sb = new StringBuilder ();
            for (int i = 1; i <= n_args; i++) {
                sb.Append (args[i]);
            }
            return Hyena.CryptoUtil.Md5Encode (sb.ToString (), System.Text.Encoding.UTF8);
        }
    }
}
