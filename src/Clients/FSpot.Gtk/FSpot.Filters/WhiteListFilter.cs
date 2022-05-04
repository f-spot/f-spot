//
// WhiteListFilter.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
// Copyright (C) 2006-2007 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace FSpot.Filters
{
	public class WhiteListFilter : IFilter
	{
		readonly List<string> valid_extensions;

		public WhiteListFilter (string[] valid_extensions)
		{
			this.valid_extensions = new List<string> ();
			foreach (string extension in valid_extensions)
				this.valid_extensions.Add (extension.ToLower ());
		}

		public bool Convert (FilterRequest req)
		{
			if (valid_extensions.Contains (Path.GetExtension (req.Current.LocalPath).ToLower ()))
				return false;

			// FIXME:  Should we add the other jpeg extensions?
			if (!valid_extensions.Contains (".jpg") &&
				!valid_extensions.Contains (".jpeg"))
				throw new System.NotImplementedException ("can only save jpeg :(");

			return (new JpegFilter ()).Convert (req);
		}
	}
}
