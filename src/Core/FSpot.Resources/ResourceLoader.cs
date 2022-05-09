//
// ResourceLoader.
//  https://github.com/PintaProject/Pinta/blob/dd226b9a3f0f82d44db0c202dacecab375efcf92/Pinta.Resources/ResourceManager.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using Gdk;

using Gtk;

namespace FSpot.Resources
{
	public static class ResourceLoader
	{
		static bool HasResource (Assembly asm, string name)
		{
			string[] resources = asm.GetManifestResourceNames ();

			if (Array.IndexOf (resources, name) > -1)
				return true;
			else
				return false;
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		public static Pixbuf GetIcon (string name, int size = -1)
		{
			Pixbuf result = null;
			try {
				// First see if it's a built-in gtk icon, like gtk-new.
				// This will also load any icons added by Gtk.IconFactory.AddDefault() .
				using (var icon_set = Widget.DefaultStyle.LookupIconSet (name)) {
					if (icon_set != null) {
						result = icon_set.RenderIcon (Widget.DefaultStyle, Widget.DefaultDirection, StateType.Normal, GetIconSize (size), null, null);
					}
				}
				// Otherwise, get it from our embedded resources.
				if (result == null) {
					var image = $"{name}-{size}.png";
					if (size == -1)
						image = $"{name}.png";
					if (HasResource (Assembly.GetExecutingAssembly (), image)) //Assembly.GetCallingAssembly() is wrong here!
						result = Pixbuf.LoadFromResource (image);
				}

				//Maybe we can find the icon in the resource of a different assembly (e.g. Plugin)
				if (result == null) {
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
						if (HasResource (asm, name))
							result = new Pixbuf (asm, name);
					}
				}
			} catch (Exception ex) {
				Console.Error.WriteLine (ex.Message);
			}

			// Ensure that we don't crash if an icon is missing for some reason.
			if (result == null) {
				try {
					// Try to return gtk's default missing image
					if (name != Stock.MissingImage) {
						result = GetIcon (Stock.MissingImage, size);
					} else {
						// If gtk is missing it's "missing image", we'll create one on the fly
						result = CreateMissingImage (size);
					}
				} catch (Exception ex) {
					Console.Error.WriteLine (ex.Message);
				}
			}
			return result;
		}

		public static Stream GetResourceIconStream (string name)
		{
			var ass = typeof (ResourceLoader).Assembly;

			return ass.GetManifestResourceStream (name);
		}

		// From MonoDevelop:
		// https://github.com/mono/monodevelop/blob/master/main/src/core/MonoDevelop.Ide/gtk-gui/generated.cs
		static Pixbuf CreateMissingImage (int size)
		{
			var pmap = new Pixmap (Screen.Default.RootWindow, size, size);
			var gc = new Gdk.GC (pmap) {
				RgbFgColor = new Color (255, 255, 255)
			};
			pmap.DrawRectangle (gc, true, 0, 0, size, size);
			gc.RgbFgColor = new Color (0, 0, 0);
			pmap.DrawRectangle (gc, false, 0, 0, (size - 1), (size - 1));

			gc.SetLineAttributes (3, LineStyle.Solid, CapStyle.Round, JoinStyle.Round);
			gc.RgbFgColor = new Color (255, 0, 0);
			pmap.DrawLine (gc, (size / 4), (size / 4), ((size - 1) - (size / 4)), ((size - 1) - (size / 4)));
			pmap.DrawLine (gc, ((size - 1) - (size / 4)), (size / 4), (size / 4), ((size - 1) - (size / 4)));

			return Pixbuf.FromDrawable (pmap, pmap.Colormap, 0, 0, 0, 0, size, size);
		}

		static Gtk.IconSize GetIconSize (int size)
		{
			return size switch {
				16 => IconSize.SmallToolbar,
				32 => IconSize.Dnd,
				_ => IconSize.Invalid,
			};
		}
	}
}