//
// RotateCommand.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2004-2006 Larry Ewing
// Copyright (C) 2008 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.UI.Dialog;
using FSpot.Utils;

using Gtk;

using Hyena;
using Hyena.Widgets;

namespace FSpot
{
	public class RotateException : ApplicationException
	{
		public bool ReadOnly;
		public string Path { get; private set; }

		public RotateException (string msg, string path) : this (msg, path, false) { }

		public RotateException (string msg, string path, bool ro) : base (msg)
		{
			Path = path;
			ReadOnly = ro;
		}
	}

	public enum RotateDirection
	{
		Clockwise,
		Counterclockwise,
	}

	public class RotateOperation
	{
		readonly IPhoto item;
		readonly RotateDirection direction;
		bool done;

		public RotateOperation (IPhoto item, RotateDirection direction)
		{
			this.item = item;
			this.direction = direction;
			done = false;
		}

		static void RotateOrientation (string original_path, RotateDirection direction)
		{
			try {
				var uri = new SafeUri (original_path);
				using var metadata = MetadataUtils.Parse (uri);

				metadata.EnsureAvailableTags ();
				var tag = metadata.ImageTag;
				var orientation = direction == RotateDirection.Clockwise
					? FSpot.Utils.PixbufUtils.Rotate90 (tag.Orientation)
					: FSpot.Utils.PixbufUtils.Rotate270 (tag.Orientation);

				tag.Orientation = orientation;
				var always_sidecar = Preferences.Get<bool> (Preferences.MetadataAlwaysUseSidecar);
				metadata.SaveSafely (uri, always_sidecar);
				App.Instance.Container.Resolve<IThumbnailService> ().DeleteThumbnails (uri);
			} catch (Exception e) {
				Logger.Log.Debug (e, "");
				throw new RotateException (Strings.UnableToRotateThisTypeOfPhoto, original_path);
			}
		}

		void Rotate (string original_path, RotateDirection dir)
		{
			RotateOrientation (original_path, dir);
		}

		public bool Step ()
		{
			if (done)
				return false;

			var attrs = File.GetAttributes (item.DefaultVersion.Uri.AbsolutePath);
			if (attrs.HasFlag (FileAttributes.ReadOnly))
				throw new RotateException (Strings.UnableToRotateReadonlyFile, item.DefaultVersion.Uri, true);

			Rotate (item.DefaultVersion.Uri, direction);

			done = true;
			return !done;
		}
	}

	public class RotateMultiple
	{
		RotateDirection direction;
		RotateOperation op;

		public int Index { get; private set; }

		public IPhoto[] Items { get; private set; }

		public RotateMultiple (IPhoto[] items, RotateDirection direction)
		{
			this.direction = direction;
			Items = items;
			Index = 0;
		}

		public bool Step ()
		{
			if (Index >= Items.Length)
				return false;

			if (op == null)
				op = new RotateOperation (Items[Index], direction);

			if (op.Step ())
				return true;
			else {
				Index++;
				op = null;
			}

			return (Index < Items.Length);
		}
	}

	public class RotateCommand
	{
		readonly Gtk.Window parent_window;

		public RotateCommand (Gtk.Window parent_window)
		{
			this.parent_window = parent_window;
		}

		public bool Execute (RotateDirection direction, IPhoto[] items)
		{
			ProgressDialog progress_dialog = null;

			if (items.Length > 1)
				progress_dialog = new ProgressDialog (Strings.RotatingPhotos,
									  ProgressDialog.CancelButtonType.Stop,
									  items.Length, parent_window);

			var op = new RotateMultiple (items, direction);
			int readonly_count = 0;
			bool done = false;
			int index = 0;

			while (!done) {
				if (progress_dialog != null && op.Index != -1 && index < items.Length)
					if (progress_dialog.Update (string.Format (Strings.RotatingPhotoX, op.Items[op.Index].Name)))
						break;

				try {
					done = !op.Step ();
				} catch (RotateException re) {
					if (!re.ReadOnly)
						RunGenericError (re, re.Path, re.Message);
					else
						readonly_count++;
				} catch (GLib.GException) {
					readonly_count++;
				} catch (DirectoryNotFoundException e) {
					RunGenericError (e, op.Items[op.Index].DefaultVersion.Uri.LocalPath, Strings.DirectoryNotFound);
				} catch (FileNotFoundException e) {
					RunGenericError (e, op.Items[op.Index].DefaultVersion.Uri.LocalPath, Strings.FileNotFonud);
				} catch (Exception e) {
					RunGenericError (e, op.Items[op.Index].DefaultVersion.Uri.LocalPath);
				}
				index++;
			}

			if (progress_dialog != null)
				progress_dialog.Destroy ();

			if (readonly_count > 0)
				RunReadonlyError (readonly_count);

			return true;
		}

		void RunReadonlyError (int readonly_count)
		{
			string notice = readonly_count <= 1 ? Strings.UnableToRotatePhoto : Strings.UnableToRotateXPhotos;
			string desc = readonly_count <= 1 ? Strings.ThePhotoCouldNotBeRotatedReadonlyOrMedia : Strings.XPhotoCouldNotBeRotatedReadonlyOrMedia;

			notice = string.Format (notice, readonly_count);
			desc = string.Format (desc, readonly_count);

			var md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Close,
										   notice, desc);
			md.Run ();
			md.Destroy ();
		}

		// FIXME shouldn't need this method, should catch all exceptions explicitly
		// so can present translated error messages.
		void RunGenericError (Exception e, string path)
		{
			RunGenericError (e, path, e.Message);
		}

		void RunGenericError (Exception e, string path, string msg)
		{
			string longmsg = string.Format (Strings.ReceivedErrorXWhileAttemptingToRotateY,
							msg, Path.GetFileName (path));

			var md = new HigMessageDialog (parent_window, DialogFlags.DestroyWithParent,
									MessageType.Warning, ButtonsType.Ok,
									Strings.ErrorWhileRotatingPhoto,
									longmsg);
			md.Run ();
			md.Destroy ();
		}
	}
}