//
// QueryDebugger.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Xml;

using Gtk;

using Hyena.Gui;

namespace Hyena.Query.Gui
{
	[TestModule ("Query Debugger")]
	public class QueryDebugger : Window
	{
		TextView input;
		TextView sql;
		TextView xml;

		QueryFieldSet query_field_set;

		public QueryDebugger () : base ("Hyena.Query Debugger")
		{
			SetDefaultSize (800, 600);

			var input_box = new VBox ();
			input_box.Spacing = 8;
			var sw = new ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.HscrollbarPolicy = PolicyType.Never;
			input = new TextView ();
			input.AcceptsTab = false;
			input.KeyReleaseEvent += delegate (object o, KeyReleaseEventArgs args) {
				if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) {
					input.Buffer.Text = input.Buffer.Text.Trim ();
					OnParseUserQuery (null, EventArgs.Empty);
				}
			};
			input.WrapMode = WrapMode.Word;
			sw.Add (input);
			input_box.PackStart (sw, true, true, 0);
			var button_box = new HBox ();
			var parse = new Button ("Parse as User Query");
			parse.Clicked += OnParseUserQuery;
			button_box.PackStart (parse, false, false, 0);
			input_box.PackStart (button_box, false, false, 0);

			var output_box = new HBox ();
			output_box.Spacing = 8;
			sw = new ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.HscrollbarPolicy = PolicyType.Never;
			sql = new TextView ();
			sql.WrapMode = WrapMode.Word;
			sw.Add (sql);
			output_box.PackStart (sw, true, true, 0);
			sw = new ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.HscrollbarPolicy = PolicyType.Never;
			xml = new TextView ();
			xml.WrapMode = WrapMode.Word;
			sw.Add (xml);
			output_box.PackStart (sw, true, true, 0);

			var pane = new VPaned ();
			pane.Add1 (input_box);
			pane.Add2 (output_box);
			pane.Position = 100;

			Add (pane);
			pane.ShowAll ();

			input.HasFocus = true;

			LoadQueryFieldSet ();
		}

		void LoadQueryFieldSet ()
		{
			var asm = Assembly.LoadFile ("Banshee.Services.dll");
			Type t = asm.GetType ("Banshee.Query.BansheeQuery");
			FieldInfo f = t.GetField ("FieldSet", BindingFlags.Public | BindingFlags.Static);
			query_field_set = (QueryFieldSet)f.GetValue (null);
		}

		StreamReader StringToStream (string s)
		{
			return new StreamReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes (s)));
		}

		void OnParseUserQuery (object o, EventArgs args)
		{
			var parser = new UserQueryParser ();
			parser.InputReader = StringToStream (input.Buffer.Text);
			QueryNode node = parser.BuildTree (query_field_set);

			sql.Buffer.Text = node.ToSql (query_field_set) ?? string.Empty;

			var doc = new XmlDocument ();
			doc.LoadXml (node.ToXml (query_field_set));

			var s = new MemoryStream ();
			var w = new XmlTextWriter (s, System.Text.Encoding.UTF8);
			w.Formatting = Formatting.Indented;
			doc.WriteContentTo (w);
			w.Flush ();
			s.Flush ();
			s.Position = 0;
			xml.Buffer.Text = new StreamReader (s).ReadToEnd ();
		}
	}
}
