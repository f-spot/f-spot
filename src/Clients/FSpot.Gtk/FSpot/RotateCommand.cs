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
using FSpot.Thumbnail;
using FSpot.UI.Dialog;

using Gtk;

using FSpot.Widgets;

namespace FSpot
{
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

			var op = new RotateMultiple (items, direction, App.Instance.Container.Resolve<IThumbnailService> ());
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
					RunGenericError (e, op.FailedPath (), Strings.DirectoryNotFound);
				} catch (FileNotFoundException e) {
					RunGenericError (e, op.FailedPath (), Strings.FileNotFonud);
				} catch (Exception e) {
					RunGenericError (e, op.FailedPath ());
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