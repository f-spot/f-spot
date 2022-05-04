//
// TestModuleRunner.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Gtk;

namespace Hyena.Gui
{
	public class TestModuleRunner : Window
	{
		public static void Run ()
		{
			Application.Init ();
			Hyena.ThreadAssist.InitializeMainThread ();
			var runner = new TestModuleRunner ();
			runner.DeleteEvent += delegate { Application.Quit (); };
			runner.ShowAll ();
			Application.Run ();
		}

		TreeStore store;

		public TestModuleRunner () : base ("Hyena.Gui Module Tester")
		{
			SetSizeRequest (-1, 300);
			Move (100, 100);

			BuildModuleList ();
			BuildView ();
		}

		void BuildModuleList ()
		{
			store = new TreeStore (typeof (string), typeof (Type));

			foreach (Type type in Assembly.GetExecutingAssembly ().GetTypes ()) {
				foreach (TestModuleAttribute attr in type.GetCustomAttributes (typeof (TestModuleAttribute), false)) {
					store.AppendValues (attr.Name, type);
				}
			}
		}

		void BuildView ()
		{
			var box = new VBox ();
			Add (box);

			var sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Never;

			var view = new TreeView ();
			view.RowActivated += delegate (object o, RowActivatedArgs args) {
				if (store.GetIter (out var iter, args.Path)) {
					var type = (Type)store.GetValue (iter, 1);
					var window = (Window)Activator.CreateInstance (type);
					window.WindowPosition = WindowPosition.Center;
					window.DeleteEvent += delegate { window.Destroy (); };
					window.Show ();
				}
			};
			view.Model = store;
			view.AppendColumn ("Module", new CellRendererText (), "text", 0);

			sw.Add (view);
			box.PackStart (sw, true, true, 0);
			sw.ShowAll ();

			var button = new Button (Stock.Quit);
			button.Clicked += delegate { Destroy (); Application.Quit (); };
			box.PackStart (button, false, false, 0);

			box.ShowAll ();
		}
	}
}
