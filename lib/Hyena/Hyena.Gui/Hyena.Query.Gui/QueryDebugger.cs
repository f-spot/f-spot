//
// QueryDebugger.cs
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
using System.IO;
using System.Xml;
using System.Reflection;
using Gtk;

using Hyena.Gui;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    [TestModule ("Query Debugger")]
    public class QueryDebugger : Window
    {
        private TextView input;
        private TextView sql;
        private TextView xml;

        private QueryFieldSet query_field_set;

        public QueryDebugger () : base ("Hyena.Query Debugger")
        {
            SetDefaultSize (800, 600);

            VBox input_box = new VBox ();
            input_box.Spacing = 8;
            ScrolledWindow sw = new ScrolledWindow ();
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
            HBox button_box = new HBox ();
            Button parse = new Button ("Parse as User Query");
            parse.Clicked += OnParseUserQuery;
            button_box.PackStart (parse, false, false, 0);
            input_box.PackStart (button_box, false, false, 0);

            HBox output_box = new HBox ();
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

            VPaned pane = new VPaned ();
            pane.Add1 (input_box);
            pane.Add2 (output_box);
            pane.Position = 100;

            Add (pane);
            pane.ShowAll ();

            input.HasFocus = true;

            LoadQueryFieldSet ();
        }

        private void LoadQueryFieldSet ()
        {
            Assembly asm = Assembly.LoadFile ("Banshee.Services.dll");
            Type t = asm.GetType ("Banshee.Query.BansheeQuery");
            FieldInfo f = t.GetField ("FieldSet", BindingFlags.Public | BindingFlags.Static);
            query_field_set = (QueryFieldSet)f.GetValue (null);
        }

        private StreamReader StringToStream (string s)
        {
            return new StreamReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes (s)));
        }

        private void OnParseUserQuery (object o, EventArgs args)
        {
            UserQueryParser parser = new UserQueryParser ();
            parser.InputReader = StringToStream (input.Buffer.Text);
            QueryNode node = parser.BuildTree (query_field_set);

            sql.Buffer.Text = node.ToSql (query_field_set) ?? String.Empty;

            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (node.ToXml (query_field_set));

            MemoryStream s = new MemoryStream ();
            XmlTextWriter w = new XmlTextWriter (s, System.Text.Encoding.UTF8);
            w.Formatting = Formatting.Indented;
            doc.WriteContentTo (w);
            w.Flush ();
            s.Flush ();
            s.Position = 0;
            xml.Buffer.Text = new StreamReader (s).ReadToEnd ();
        }
    }
}
