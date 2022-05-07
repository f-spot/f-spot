//
// DbItem.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Models;

namespace FSpot.Database
{
	public class DbItemEventArgs<T> : EventArgs where T : BaseDbSet
	{
		public List<T> Items { get; }

		public DbItemEventArgs (List<T> items)
		{
			Items = items;
		}

		public DbItemEventArgs (T item)
		{
			Items = new List<T> { item };
		}
	}
}