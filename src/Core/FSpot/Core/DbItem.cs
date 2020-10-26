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

namespace FSpot.Core
{
	public class DbItem
	{
		public uint Id { get; private set; }

		protected DbItem (uint id)
		{
			Id = id;
		}
	}

	public class DbItemEventArgs<T> : EventArgs where T : DbItem
	{
		public T[] Items { get; private set; }

		public DbItemEventArgs (T[] items)
		{
			Items = items;
		}

		public DbItemEventArgs (T item)
		{
			Items = new T[] { item };
		}
	}
}