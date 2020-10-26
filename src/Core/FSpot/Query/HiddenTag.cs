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

using FSpot.Core;

namespace FSpot.Query
{
	public class HiddenTag : IQueryCondition
	{
		static HiddenTag showHiddenTag;
		static HiddenTag hideHiddenTag;
		readonly bool showHidden;

		public static Tag Tag { get; set; }

		public static HiddenTag ShowHiddenTag {
			get {
				if (showHiddenTag == null)
					showHiddenTag = new HiddenTag (true);

				return showHiddenTag;
			}
		}

		public static HiddenTag HideHiddenTag {
			get {
				if (hideHiddenTag == null)
					hideHiddenTag = new HiddenTag (false);

				return hideHiddenTag;
			}
		}

		HiddenTag (bool showHidden)
		{
			this.showHidden = showHidden;
		}

		public string SqlClause ()
		{
			if (!showHidden && Tag != null)
				return $" photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {Tag.Id}) ";

			return null;
		}
	}
}
