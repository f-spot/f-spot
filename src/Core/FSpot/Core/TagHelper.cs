//
// TagHelper.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2013-2020 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Models;
using FSpot.Settings;
using FSpot.Utils;
using Gdk;

namespace FSpot.Core
{
	public class TagHelper : IDisposable
	{
		bool disposed;
		Pixbuf icon;
		Pixbuf cached_icon;
		IconSize cached_icon_size = IconSize.Hidden;

		Tag tag;

		public TagHelper (Tag tag)
		{
			this.tag = tag;
		}
		public Pixbuf Icon {
			get {
				if (icon == null && tag.ThemeIconName != null) {
					cached_icon_size = IconSize.Hidden;
					icon = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, tag.ThemeIconName, 48, (Gtk.IconLookupFlags)0);
				}
				return icon;
			}
			set {
				tag.ThemeIconName = null;
				icon?.Dispose ();
				icon = value;
				cached_icon_size = IconSize.Hidden;
				tag.IconWasCleared = value == null;
			}
		}

		// We can use a SizedIcon everywhere we were using an Icon
		public Pixbuf SizedIcon {
			get {
				if (Tag.TagIconSize == IconSize.Hidden) //Hidden
					return null;

				if (Tag.TagIconSize == cached_icon_size)
					return cached_icon;

				if (tag.ThemeIconName != null) { //Theme icon
					cached_icon?.Dispose ();

					try {
						cached_icon = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, tag.ThemeIconName, (int)Tag.TagIconSize, (Gtk.IconLookupFlags)0);

						if (Math.Max (cached_icon.Width, cached_icon.Height) <= (int)Tag.TagIconSize)
							return cached_icon;
					} catch (Exception) {
						Console.WriteLine ($"missing theme icon: {tag.ThemeIconName}");
						return null;
					}
				}

				if (Icon == null)
					return null;

				if (Math.Max (Icon.Width, Icon.Height) >= (int)Tag.TagIconSize) { //Don't upscale
					cached_icon?.Dispose ();
					var tagIconSize = (int)Tag.TagIconSize;
					cached_icon = Icon.ScaleSimple (tagIconSize, tagIconSize, InterpType.Bilinear);
					cached_icon_size = Tag.TagIconSize;
					return cached_icon;
				}

				return Icon;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// free managed resources
				if (icon != null) {
					icon.Dispose ();
					icon = null;
				}
				if (cached_icon != null) {
					cached_icon.Dispose ();
					cached_icon = null;
				}
			}
			// free unmanaged resources
		}
	}
}
