//
// TestModuleRunner.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
            TestModuleRunner runner = new TestModuleRunner ();
            runner.DeleteEvent += delegate { Application.Quit (); };
            runner.ShowAll ();
            Application.Run ();
        }

        private TreeStore store;

        public TestModuleRunner () : base ("Hyena.Gui Module Tester")
        {
            SetSizeRequest (-1, 300);
            Move (100, 100);

            BuildModuleList ();
            BuildView ();
        }

        private void BuildModuleList ()
        {
            store = new TreeStore (typeof (string), typeof (Type));

            foreach (Type type in Assembly.GetExecutingAssembly ().GetTypes ()) {
                foreach (TestModuleAttribute attr in type.GetCustomAttributes (typeof (TestModuleAttribute), false)) {
                    store.AppendValues (attr.Name, type);
                }
            }
        }

        private void BuildView ()
        {
            VBox box = new VBox ();
            Add (box);

            ScrolledWindow sw = new ScrolledWindow ();
            sw.HscrollbarPolicy = PolicyType.Never;

            TreeView view = new TreeView ();
            view.RowActivated += delegate (object o, RowActivatedArgs args) {
                TreeIter iter;
                if (store.GetIter (out iter, args.Path)) {
                    Type type = (Type)store.GetValue (iter, 1);
                    Window window = (Window)Activator.CreateInstance (type);
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

            Button button = new Button (Stock.Quit);
            button.Clicked += delegate { Destroy (); Application.Quit (); };
            box.PackStart (button, false, false, 0);

            box.ShowAll ();
        }
    }
}
