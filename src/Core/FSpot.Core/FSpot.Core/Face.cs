//
// Face.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Valentín Barros <valentin@sanva.net>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2013 Valentín Barros.
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

using System;

using Gdk;

using FSpot.Utils;

using Hyena;

namespace FSpot.Core
{
	public class Face : DbItem, IComparable<Face>, IDisposable
	{
		public string Name { get; set; }
		public bool IconWasCleared { get; set; }

		Pixbuf icon;
		public Pixbuf Icon {
			get { return icon; }
			set {
				if (icon != null)
					icon.Dispose ();
				icon = value;
				cached_icon_size = IconSize.Hidden;
				IconWasCleared = value == null;
			}
		}

		public enum IconSize {
			Hidden = 0,
			Small = 16,
			Medium = 24,
			Large = 48
		};

		static IconSize face_icon_size = IconSize.Large;
		public static IconSize FaceIconSize {
			get { return face_icon_size; }
			set { face_icon_size = value; }
		}

		Pixbuf cached_icon;
		private IconSize cached_icon_size = IconSize.Hidden;

		// We can use a SizedIcon everywhere we were using an Icon
		public Pixbuf SizedIcon {
			get {
				if (face_icon_size == IconSize.Hidden) //Hidden
					return null;
				if (face_icon_size == cached_icon_size)
					return cached_icon;
				if (Icon == null)
					return null;

				if (Math.Max (Icon.Width, Icon.Height) >= (int) face_icon_size) { //Don't upscale
					if (cached_icon != null)
						cached_icon.Dispose ();
					cached_icon = Icon.ScaleSimple ((int) face_icon_size, (int) face_icon_size, InterpType.Bilinear);
					cached_icon_size = face_icon_size;
					return cached_icon;
				} else
					return Icon;
			}
		}

		public Face (uint id, string name)
			: base (id)
		{
			Name = name;
			IconWasCleared = false;
		}


		// IComparer
		public int CompareTo (Face face)
		{
			return String.Compare (Name, face.Name, StringComparison.CurrentCulture);
		}

		public void Dispose ()
		{
			if (icon != null)
				icon.Dispose ();
			if (cached_icon != null)
				cached_icon.Dispose ();
			System.GC.SuppressFinalize (this);
		}

		~Face ()
		{
			Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
			if (icon != null)
				icon.Dispose ();
			if (cached_icon != null)
				cached_icon.Dispose ();
		}
	}
}
