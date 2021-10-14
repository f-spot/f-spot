//
// HiddenTag.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using FSpot.Models;

namespace FSpot.Query
{
	public class HiddenTag : IQueryCondition
	{
		static HiddenTag show_hidden_tag;
		static HiddenTag hide_hidden_tag;
		readonly bool show_hidden;

		public static Tag Tag { get; set;}

		public static HiddenTag ShowHiddenTag {
			get {
				if (show_hidden_tag == null)
					show_hidden_tag = new HiddenTag (true);

				return show_hidden_tag;
			}
		}

		public static HiddenTag HideHiddenTag {
			get {
				if (hide_hidden_tag == null)
					hide_hidden_tag = new HiddenTag (false);

				return hide_hidden_tag;
			}
		}

		HiddenTag (bool showHidden)
		{
			show_hidden = showHidden;
		}

		public string SqlClause ()
		{
			if (!show_hidden && Tag != null)
				return $" photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {Tag.Id} ";
			return null;
		}
	}
}
