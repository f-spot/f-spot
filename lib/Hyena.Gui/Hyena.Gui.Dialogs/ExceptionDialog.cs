//
// ExceptionDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Resources;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using Gtk;

namespace Hyena.Gui.Dialogs
{
    public class ExceptionDialog : Dialog
    {
        private AccelGroup accel_group;
        private string debugInfo;

        public ExceptionDialog(Exception e) : base()
        {
            debugInfo = BuildExceptionMessage(e);

            HasSeparator = false;
            BorderWidth = 5;
            Resizable = false;
            //Translators: {0} is substituted with the application name
            Title = String.Format(Catalog.GetString("{0} Encountered a Fatal Error"),
                                  ApplicationContext.ApplicationName);

            VBox.Spacing = 12;
            ActionArea.Layout = ButtonBoxStyle.End;

            accel_group = new AccelGroup();
            AddAccelGroup(accel_group);

            HBox hbox = new HBox(false, 12);
            hbox.BorderWidth = 5;
            VBox.PackStart(hbox, false, false, 0);

            Image image = new Image(Stock.DialogError, IconSize.Dialog);
            image.Yalign = 0.0f;
            hbox.PackStart(image, true, true, 0);

            VBox label_vbox = new VBox(false, 0);
            label_vbox.Spacing = 12;
            hbox.PackStart(label_vbox, false, false, 0);

            Label label = new Label(String.Format("<b><big>{0}</big></b>", GLib.Markup.EscapeText(Title)));
            label.UseMarkup = true;
            label.Justify = Justification.Left;
            label.LineWrap = true;
            label.SetAlignment(0.0f, 0.5f);
            label_vbox.PackStart(label, false, false, 0);

            label = new Label(e.Message);

            label.UseMarkup = true;
            label.UseUnderline = false;
            label.Justify = Gtk.Justification.Left;
            label.LineWrap = true;
            label.Selectable = true;
            label.SetAlignment(0.0f, 0.5f);
            label_vbox.PackStart(label, false, false, 0);

            Label details_label = new Label(String.Format("<b>{0}</b>",
                GLib.Markup.EscapeText(Catalog.GetString("Error Details"))));
            details_label.UseMarkup = true;
            Expander details_expander = new Expander("Details");
            details_expander.LabelWidget = details_label;
            label_vbox.PackStart(details_expander, true, true, 0);

            ScrolledWindow scroll = new ScrolledWindow();
            TextView view = new TextView();

            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;
            scroll.AddWithViewport(view);

            scroll.SetSizeRequest(450, 250);

            view.Editable = false;
            view.Buffer.Text = debugInfo;

            details_expander.Add(scroll);

            hbox.ShowAll();

            AddButton(Stock.Close, ResponseType.Close, true);
        }

        private void AddButton(string stock_id, Gtk.ResponseType response, bool is_default)
        {
            Button button = new Button(stock_id);
            button.CanDefault = true;
            button.Show ();

            AddActionWidget(button, response);

            if(is_default) {
                DefaultResponse = response;
                button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Return,
                    0, AccelFlags.Visible);
            }
        }

        private string BuildExceptionMessage(Exception e)
        {
            System.Text.StringBuilder msg = new System.Text.StringBuilder();

            msg.Append(Catalog.GetString("An unhandled exception was thrown: "));

            Stack<Exception> exception_chain = new Stack<Exception> ();

            while (e != null) {
                exception_chain.Push (e);
                e = e.InnerException;
            }

            while (exception_chain.Count > 0) {
                e = exception_chain.Pop ();
                msg.AppendFormat ("{0}\n\n{1}\n", e.Message, e.StackTrace);
            };

            msg.Append("\n");
            msg.AppendFormat(".NET Version: {0}\n", Environment.Version);
            msg.AppendFormat("OS Version: {0}\n", Environment.OSVersion);
            msg.Append("\nAssembly Version Information:\n\n");

            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                AssemblyName name = asm.GetName();
                msg.AppendFormat("{0} ({1})\n", name.Name, name.Version);
            }

            if(Environment.OSVersion.Platform != PlatformID.Unix) {
                return msg.ToString();
            }

            try {
                msg.AppendFormat("\nPlatform Information: {0}", BuildPlatformString());

                msg.Append("\n\nDisribution Information:\n\n");

                Dictionary<string, string> lsb = LsbVersionInfo.Harvest;

                foreach(string lsbfile in lsb.Keys) {
                    msg.AppendFormat("[{0}]\n", lsbfile);
                    msg.AppendFormat("{0}\n", lsb[lsbfile]);
                }
            } catch {
            }

            return msg.ToString();
        }

        private string BuildPlatformString()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.Arguments = "-sirom";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;

            foreach(string unameprog in new string [] {
                "/usr/bin/uname", "/bin/uname", "/usr/local/bin/uname",
                "/sbin/uname", "/usr/sbin/uname", "/usr/local/sbin/uname"}) {
                try {
                    startInfo.FileName = unameprog;
                    Process uname = Process.Start(startInfo);
                    return uname.StandardOutput.ReadLine().Trim();
                } catch(Exception) {
                    continue;
                }
            }

            return null;
        }

        private class LsbVersionInfo
        {
            private string [] filesToCheck = {
                "*-release",
                "slackware-version",
                "debian_version"
            };

            private Dictionary<string, string> harvest = new Dictionary<string, string>();

            public LsbVersionInfo()
            {
                foreach(string pattern in filesToCheck) {
                    foreach(string filename in Directory.GetFiles("/etc/", pattern)) {
                        using(FileStream fs = File.OpenRead(filename)) {
                            harvest[filename] = (new StreamReader(fs)).ReadToEnd();
                        }
                    }
                }
            }

            public Dictionary<string, string> Findings {
                get { return harvest; }
            }

            public static Dictionary<string, string> Harvest {
                get { return (new LsbVersionInfo()).Findings; }
            }
        }
    }
}
