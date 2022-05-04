//
// Mono.Facebook.FacebookParam.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolvce.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Mono.Facebook
{
	public class FacebookParam : IComparable
	{
		private string name;
		private object value;

		public string Name {
			get{ return name; }
		}

		public string Value {
			get {
				if (value is Array)
					return ConvertArrayToString (value as Array);
				else
					return value.ToString ();
			}
		}

		protected FacebookParam (string name, object value)
		{
			this.name = name;
			this.value = value;
		}

		public override string ToString ()
		{
			return string.Format ("{0}={1}", Name, Value);
		}

		public static FacebookParam Create (string name, object value)
		{
			return new FacebookParam (name, value);
		}

		public int CompareTo (object obj)
		{
			if (!(obj is FacebookParam))
				return -1;

			return this.name.CompareTo ((obj as FacebookParam).name);
		}

		private static string ConvertArrayToString (Array a)
		{
			StringBuilder builder = new StringBuilder ();

			for (int i = 0; i < a.Length; i++) {
				if (i > 0)
					builder.Append (",");

				builder.Append (a.GetValue (i).ToString ());
			}

			return builder.ToString ();
		}
	}
}
