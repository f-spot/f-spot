//
// ExceptionDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Gui.Dialogs
{
	public class ExceptionDialog : Dialog
	{
		AccelGroup accel_group;
		string debugInfo;

		public ExceptionDialog (Exception e) : base ()
		{
			debugInfo = BuildExceptionMessage (e);

			HasSeparator = false;
			BorderWidth = 5;
			Resizable = false;
			//Translators: {0} is substituted with the application name
			Title = string.Format (Strings.XEncounteredAFatalError, ApplicationContext.ApplicationName);

			VBox.Spacing = 12;
			ActionArea.Layout = ButtonBoxStyle.End;

			accel_group = new AccelGroup ();
			AddAccelGroup (accel_group);

			var hbox = new HBox (false, 12);
			hbox.BorderWidth = 5;
			VBox.PackStart (hbox, false, false, 0);

			var image = new Image (Stock.DialogError, IconSize.Dialog);
			image.Yalign = 0.0f;
			hbox.PackStart (image, true, true, 0);

			var label_vbox = new VBox (false, 0);
			label_vbox.Spacing = 12;
			hbox.PackStart (label_vbox, false, false, 0);

			var label = new Label (string.Format ("<b><big>{0}</big></b>", GLib.Markup.EscapeText (Title)));
			label.UseMarkup = true;
			label.Justify = Justification.Left;
			label.LineWrap = true;
			label.SetAlignment (0.0f, 0.5f);
			label_vbox.PackStart (label, false, false, 0);

			label = new Label (e.Message);

			label.UseMarkup = true;
			label.UseUnderline = false;
			label.Justify = Gtk.Justification.Left;
			label.LineWrap = true;
			label.Selectable = true;
			label.SetAlignment (0.0f, 0.5f);
			label_vbox.PackStart (label, false, false, 0);

			var details_label = new Label ($"<b>{GLib.Markup.EscapeText (Strings.ErrorDetails)}</b>");
			details_label.UseMarkup = true;
			var details_expander = new Expander ("Details");
			details_expander.LabelWidget = details_label;
			label_vbox.PackStart (details_expander, true, true, 0);

			var scroll = new ScrolledWindow ();
			var view = new TextView ();

			scroll.HscrollbarPolicy = PolicyType.Automatic;
			scroll.VscrollbarPolicy = PolicyType.Automatic;
			scroll.AddWithViewport (view);

			scroll.SetSizeRequest (450, 250);

			view.Editable = false;
			view.Buffer.Text = debugInfo;

			details_expander.Add (scroll);

			hbox.ShowAll ();

			AddButton (Stock.Close, ResponseType.Close, true);
		}

		void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
		{
			var button = new Button (stock_id);
			button.CanDefault = true;
			button.Show ();

			AddActionWidget (button, response);

			if (is_default) {
				DefaultResponse = response;
				button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Return,
					0, AccelFlags.Visible);
			}
		}

		string BuildExceptionMessage (Exception e)
		{
			var msg = new System.Text.StringBuilder ();

			msg.Append (Strings.AnUnhandledExceptionWasThrown);

			var exception_chain = new Stack<Exception> ();

			while (e != null) {
				exception_chain.Push (e);
				e = e.InnerException;
			}

			while (exception_chain.Count > 0) {
				e = exception_chain.Pop ();
				msg.AppendFormat ("{0}\n\n{1}\n", e.Message, e.StackTrace);
			};

			msg.Append ('\n');
			msg.AppendFormat (".NET Version: {0}\n", Environment.Version);
			msg.AppendFormat ("OS Version: {0}\n", Environment.OSVersion);
			msg.Append ("\nAssembly Version Information:\n\n");

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
				AssemblyName name = asm.GetName ();
				msg.AppendFormat ("{0} ({1})\n", name.Name, name.Version);
			}

			if (Environment.OSVersion.Platform != PlatformID.Unix) {
				return msg.ToString ();
			}

			try {
				msg.AppendFormat ("\nPlatform Information: {0}", BuildPlatformString ());

				msg.Append ("\n\nDisribution Information:\n\n");

				Dictionary<string, string> lsb = LsbVersionInfo.Harvest;

				foreach (string lsbfile in lsb.Keys) {
					msg.AppendFormat ("[{0}]\n", lsbfile);
					msg.AppendFormat ("{0}\n", lsb[lsbfile]);
				}
			} catch {
			}

			return msg.ToString ();
		}

		string BuildPlatformString ()
		{
			var startInfo = new ProcessStartInfo ();
			startInfo.Arguments = "-sirom";
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;

			foreach (string unameprog in new string[] {
				"/usr/bin/uname", "/bin/uname", "/usr/local/bin/uname",
				"/sbin/uname", "/usr/sbin/uname", "/usr/local/sbin/uname"}) {
				try {
					startInfo.FileName = unameprog;
					var uname = Process.Start (startInfo);
					return uname.StandardOutput.ReadLine ().Trim ();
				} catch (Exception) {
					continue;
				}
			}

			return null;
		}

		class LsbVersionInfo
		{
			string[] filesToCheck = {
				"*-release",
				"slackware-version",
				"debian_version"
			};

			Dictionary<string, string> harvest = new Dictionary<string, string> ();

			public LsbVersionInfo ()
			{
				foreach (string pattern in filesToCheck) {
					foreach (string filename in Directory.GetFiles ("/etc/", pattern)) {
						using (FileStream fs = File.OpenRead (filename)) {
							harvest[filename] = (new StreamReader (fs)).ReadToEnd ();
						}
					}
				}
			}

			public Dictionary<string, string> Findings {
				get { return harvest; }
			}

			public static Dictionary<string, string> Harvest {
				get { return (new LsbVersionInfo ()).Findings; }
			}
		}
	}
}
