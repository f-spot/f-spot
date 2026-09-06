//
// CleanRoomStartup.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Hyena.Widgets;

namespace Hyena.Gui
{
	public static class CleanRoomStartup
	{
		public delegate void StartupInvocationHandler ();

		public static void Startup (StartupInvocationHandler startup)
		{
			bool disable_clean_room = false;

			foreach (string arg in Environment.GetCommandLineArgs ()) {
				if (arg == "--disable-clean-room") {
					disable_clean_room = true;
					break;
				}
			}

			if (disable_clean_room) {
				startup ();
				return;
			}

			try {
				startup ();
			} catch (Exception e) {
				Console.WriteLine (e);

				Gtk.Application.Init ();
				var dialog = new HigMessageDialog (
					null, Gtk.DialogFlags.Modal,
					Gtk.MessageType.Error, Gtk.ButtonsType.Close,
					Strings.UnhandledException,
					e.ToString ());
				dialog.Run ();
				dialog.Destroy ();
				Environment.Exit (1);
			}
		}
	}
}
