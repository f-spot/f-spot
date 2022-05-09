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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Models;

namespace FSpot.Query
{
	public class HiddenTag : IQueryCondition
	{
		static HiddenTag show_hidden_tag;
		static HiddenTag hide_hidden_tag;
		readonly bool show_hidden;

		public static Tag Tag { get; set; }

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
				return $" photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {Tag.Id}) ";
			return null;
		}
	}
}
