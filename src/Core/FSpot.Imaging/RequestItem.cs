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

using FSpot.Utils;
using Gdk;
using Hyena;

namespace FSpot
{
	public class RequestItem
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
			get {
				return result == null ? null : result.ShallowCopy ();
			}
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
			if ((width <= 0 && height > 0) || (height <= 0 && width > 0)) {
				throw new System.Exception ("Invalid arguments");
			}
		}

		~RequestItem ()
		{
			if (result != null) {
				result.Dispose ();
			}
			result = null;
		}
	}
}
