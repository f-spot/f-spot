//
// RequestItem.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//   Ettore Perazzoli <ettore@src.gnome.org>
//
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2003-2006 Larry Ewing
// Copyright (C) 2003 Ettore Perazzoli
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Utils;

using Gdk;

using Hyena;

namespace FSpot
{
	public class RequestItem : IDisposable
	{
		/// <summary>
		/// Gets or Sets image uri
		/// </summary>
		/// <value>
		/// Image Uri
		/// </value>
		public SafeUri Uri { get; set; }

		/* Order value; requests with a lower value get performed first.  */
		public int Order { get; set; }

		/* The pixbuf obtained from the operation.  */
		Pixbuf result;

		public Pixbuf Result {
			get => result?.ShallowCopy ();
			set { result = value; }
		}

		/* the maximium size both must be greater than zero if either is */
		public int Width { get; set; }

		public int Height { get; set; }

		public RequestItem (SafeUri uri, int order, int width, int height)
		{
			Uri = uri;
			Order = order;
			Width = width;
			Height = height;

			if ((width <= 0 && height > 0) || (height <= 0 && width > 0))
				throw new Exception ("Invalid arguments");
		}

		public void Dispose ()
		{
			Dispose (true);
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				if (result != null) {
					result.Dispose ();
				}
			}
		}
	}
}
