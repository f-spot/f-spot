using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Collections;
using System.IO;
using Mono.Unix;
using Gtk;

namespace FSpot
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
            Title = Catalog.GetString("F-Spot Encountered a Fatal Error");
            
            VBox.Spacing = 12;
            ActionArea.Layout = ButtonBoxStyle.End;

            accel_group = new AccelGroup();
		    AddAccelGroup(accel_group);
        
            HBox hbox = new HBox(false, 12);
            hbox.BorderWidth = 5;
            VBox.PackStart(hbox, false, false, 0);
        
            Image image = new Image(Stock.DialogError, IconSize.Dialog);
            image.Yalign = 0.05f;
            hbox.PackStart(image, true, true, 0);

            VBox label_vbox = new VBox(false, 0);
            label_vbox.Spacing = 12;
            hbox.PackStart(label_vbox, false, false, 0);

            Label label = new Label("<b><big>" + Title + "</big></b>");
            label.UseMarkup = true;
            label.Justify = Justification.Left;
            label.LineWrap = true;
            label.SetAlignment(0.0f, 0.5f);
            label_vbox.PackStart(label, false, false, 0);

            label = new Gtk.Label(Catalog.GetString(
                "This may be due to a programming error. Please help us make F-Spot better " + 
                "by reporting this error. Thank you in advance!"));
                
            label.UseMarkup = true;
            label.Justify = Gtk.Justification.Left;
            label.LineWrap = true;
            label.SetAlignment(0.0f, 0.5f);
            label_vbox.PackStart(label, false, false, 0);

            ScrolledWindow scroll = new ScrolledWindow();
            TextView view = new TextView();
            
            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;
            scroll.AddWithViewport(view);
            
            scroll.SetSizeRequest(450, 250);
			
			view.Editable = false;
			view.Buffer.Text = debugInfo;
			
			label_vbox.PackStart(scroll, true, true, 0);
			
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
                button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Escape, 
                    0, AccelFlags.Visible);
            }
        }
        
        private string BuildExceptionMessage(Exception e)
        {
            System.Text.StringBuilder msg = new System.Text.StringBuilder();
            
            msg.Append(Catalog.GetString("An unhandled exception was thrown: "));
            msg.Append(e.Message);
            
            msg.Append("\n\n");
            msg.Append(e.StackTrace);
            
            msg.Append("\n");
            
            msg.Append(".NET Version: " + Environment.Version.ToString());
            
            msg.Append("\n\nAssembly Version Information:\n\n");
            
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
				AssemblyName name = asm.GetName();
                msg.Append(name.Name + " (" + name.Version.ToString() + ")\n");
			}
            
            msg.Append("\nPlatform Information: " + BuildPlatformString());
            
            msg.Append("\n\nDisribution Information:\n\n");
            
            Hashtable lsb = LsbVersionInfo.Harvest;
            
            foreach(string lsbfile in lsb.Keys) {
                msg.Append("[" + lsbfile + "]\n");
                msg.Append(lsb[lsbfile] + "\n");
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
	}
	
    public class LsbVersionInfo
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
        
        public Hashtable Findings
        {
            get {
                return harvest;
            }
        }
        
        public static Hashtable Harvest
        {
            get {
                return (new LsbVersionInfo()).Findings;
            }
        }
    }
}
