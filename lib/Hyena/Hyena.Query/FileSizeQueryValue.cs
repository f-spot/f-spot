//
// FileSizeQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Xml;

namespace Hyena.Query
{
	public enum FileSizeFactor : long
	{
		None = 1,
		KB = 1024,
		MB = 1048576,
		GB = 1073741824,
		TB = 1099511627776,
		PB = 1125899906842624
	}

	public class FileSizeQueryValue : IntegerQueryValue
	{
		FileSizeFactor factor = FileSizeFactor.None;
		public FileSizeFactor Factor {
			get { return factor; }
		}

		public FileSizeQueryValue ()
		{
		}

		public FileSizeQueryValue (long bytes)
		{
			value = bytes;
			IsEmpty = false;
			DetermineFactor ();
		}

		public double FactoredValue {
			get { return (double)value / (double)factor; }
		}

		public override void ParseUserQuery (string input)
		{
			if (input.Length > 1 && (input[input.Length - 1] == 'b' || input[input.Length - 1] == 'B')) {
				input = input.Substring (0, input.Length - 1);
			}

			IsEmpty = !double.TryParse (input, out var double_value);

			if (IsEmpty && input.Length > 1) {
				IsEmpty = !double.TryParse (input.Substring (0, input.Length - 1), out double_value);
			}

			if (!IsEmpty) {
				switch (input[input.Length - 1]) {
				case 'k': case 'K': factor = FileSizeFactor.KB; break;
				case 'm': case 'M': factor = FileSizeFactor.MB; break;
				case 'g': case 'G': factor = FileSizeFactor.GB; break;
				case 't': case 'T': factor = FileSizeFactor.TB; break;
				case 'p': case 'P': factor = FileSizeFactor.PB; break;
				default: factor = FileSizeFactor.None; break;
				}
				value = (long)((double)factor * double_value);
			}
		}

		public override void ParseXml (XmlElement node)
		{
			base.ParseUserQuery (node.InnerText);
			if (node.HasAttribute ("factor")) {
				factor = (FileSizeFactor)Enum.Parse (typeof (FileSizeFactor), node.GetAttribute ("factor"));
			} else {
				DetermineFactor ();
			}
		}

		public override void AppendXml (XmlElement node)
		{
			base.AppendXml (node);
			node.SetAttribute ("factor", factor.ToString ());
		}

		public void SetValue (double value, FileSizeFactor factor)
		{
			this.value = (long)(value * (double)factor);
			this.factor = factor;
			IsEmpty = false;
		}

		protected void DetermineFactor ()
		{
			if (!IsEmpty && value != 0) {
				foreach (FileSizeFactor factor in Enum.GetValues (typeof (FileSizeFactor))) {
					if (value >= (double)factor) {
						this.factor = factor;
					}
				}
			}
		}

		public override string ToUserQuery ()
		{
			return ToUserQuery (false);
		}

		public string ToUserQuery (bool always_decimal)
		{
			if (factor != FileSizeFactor.None) {
				return string.Format ("{0} {1}",
					IntValue == 0
						? "0"
						: StringUtil.DoubleToTenthsPrecision (((double)IntValue / (double)factor), always_decimal),
					factor.ToString ()
				);
			} else {
				return base.ToUserQuery ();
			}
		}
	}
}
