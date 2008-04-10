using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Collections;
using System.IO;
using Mono.Unix;
using Gtk;

namespace FSpot.UI.Dialog
{
    public class ExceptionDialog : Gtk.Dialog
    {
        private AccelGroup accel_group;
        private string debugInfo;
        
        public ExceptionDialog(Exception e) : base()
        {
            debugInfo = BuildExceptionMessage(e);
           
            HasSeparator = false;
            BorderWidth = 5;
            Resizable = false;
            Title = Catalog.GetString("F-Spot Encountered a Fatal Error");
            
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
            label.Justify = Gtk.Justification.Left;
            label.LineWrap = true;
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
            button.Show();

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
            msg.Append(e.Message);
            
            msg.Append(String.Format("{0}{0}", Environment.NewLine));
            msg.Append(e.StackTrace);
            
            msg.Append(Environment.NewLine);
            
            msg.Append(".NET Version: " + Environment.Version.ToString());
            
            msg.Append(String.Format("{0}{0}Assembly Version Information:{0}{0}", Environment.NewLine));
            
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
		    AssemblyName name = asm.GetName();
                    msg.Append(name.Name + " (" + name.Version.ToString () + ")" + Environment.NewLine);
		}
            
            msg.Append(Environment.NewLine + "Platform Information: " + BuildPlatformString());
            
            msg.Append(String.Format("{0}{0}Distribution Information:{0}{0}", Environment.NewLine));
            
            Hashtable lsb = LsbVersionInfo.Harvest;
            
            foreach(string lsbfile in lsb.Keys) {
                msg.Append("[" + lsbfile + "]" + Environment.NewLine);
                msg.Append(lsb[lsbfile] + Environment.NewLine);
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
            
            private Hashtable harvest = new Hashtable(); 
            
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
            
            public Hashtable Findings {
                get { return harvest; }
            }
            
            public static Hashtable Harvest {
                get { return (new LsbVersionInfo()).Findings; }
            }
        }
    }
}
