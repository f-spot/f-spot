using Gtk;
using Gnome;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using Mono.Unix;

namespace FSpot 
{

public class Driver {
	public static void Main (string [] args)
	{
		bool view_only = false;
		bool import = false;
		bool empty = false;
		Program program = null;
		CoreControl control = null;

		SetProcessName (Defines.PACKAGE);
		
		try {
			foreach (string arg in args) {
				if (arg == "--help") {
					Catalog.Init ("f-spot", Defines.LOCALE_DIR);

					System.Console.WriteLine ("Usage f-spot [OPTION. ..]\n");
					System.Console.WriteLine ("  --import [uri]\t\t\timport from the given uri");
					System.Console.WriteLine ("  --view <file>\t\t\t\tview a file or directory ");
					System.Console.WriteLine ("  --shutdown\t\t\t\tshutdown a running f-spot server");
					System.Console.WriteLine ("  --slideshow\t\t\t\tdisplay a slideshow");
					System.Console.WriteLine ("  --debug\t\t\t\trun f-spot with mono in debug mode");
					System.Console.WriteLine ("  --help\t\t\t\tview this message");
					
					System.Console.WriteLine ("");
					program = new Program (Defines.PACKAGE, 
							       Defines.VERSION, 
							       Modules.UI, args);
					return;
				} else if (arg == "--slideshow") {
					Catalog.Init ("f-spot", Defines.LOCALE_DIR);
						
					program = new Program (Defines.PACKAGE, 
							       Defines.VERSION, 
						       Modules.UI, args);
					Core core = new Core ();
					core.ShowSlides (null);
					program.Run ();
					System.Console.WriteLine ("done");
					return;
				}
			}
			
			/* 
			 * FIXME we need to inialize gobject before making the dbus calls, we'll go 
			 * ahead and do it like this for now.
			 */ 
			program = new Program (Defines.PACKAGE, 
					       Defines.VERSION, 
					       Modules.UI, args);		
			
			bool create = true;
			while (control == null) {
				try {
					control = Core.FindInstance ();
					System.Console.WriteLine ("Found active FSpot server: {0}", control);
					program = null;
				} catch (System.Exception e) { 
					Core.AssertOwnership ();
					System.Console.WriteLine ("Starting new FSpot server");
				}
			
				Core core = null;
				try {
					if (control == null && create) {
						create = false;
						Gnome.Vfs.Vfs.Initialize ();
						StockIcons.Initialize ();
						
						Catalog.Init ("f-spot", Defines.LOCALE_DIR);
						Gtk.Window.DefaultIconList = new Gdk.Pixbuf [] {PixbufUtils.LoadFromAssembly ("f-spot-logo.png")};
						
						core = new Core ();
						core.RegisterServer ();
						
						// FIXME: Error checking is non-existant here...
						
						empty = Core.Database.Empty;
						control = core;
					}
				} catch (System.Exception e) {
					System.Console.WriteLine ("XXXXX\n{0}\nXXXXX", e);
					control = null;

					if (core != null)
						core.UnregisterServer ();
				}
			}
			
			
			UriList list = new UriList ();
			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
				case "--shutdown":
					control.Shutdown ();
					break;
				case "--import":
					if (++i < args.Length)
						control.Import (args [i]);
					
					import = true;
					break;
				case "--view":
					while (++i < args.Length)
						list.AddUnknown (args [i]);
					
					if (list.Count > 0)
						control.View (list.ToString ());

					view_only = true;
					break;
				}
			}
			
			if (empty && !import)
				control.Import (null);
			
			if (import || !view_only)
				control.Organize ();
			
			if (program != null)
				program.Run ();
			
			System.Console.WriteLine ("exiting");
		} catch (System.Exception e) {
			Console.Error.WriteLine(e);
			Gtk.Application.Init();
			ExceptionDialog dlg = new ExceptionDialog(e);
			dlg.Run();
			dlg.Destroy();
			System.Environment.Exit(1);
		}
	}

	[DllImport("libc")]
	private static extern int prctl(int option, string name, ulong arg3,
					ulong arg4, ulong arg5);
	
	public static void SetProcessName(string name)
	{
		try {
			if(prctl(15 /* PR_SET_NAME */, name,
				 0, 0, 0) != 0) {
				throw new ApplicationException("Error setting process name: " +
							       Mono.Unix.Native.Stdlib.GetLastError());
			}
		} catch (DllNotFoundException de) {
			/* noop */
		} catch (EntryPointNotFoundException de) {
		    	/* noop */
		}
	}
}

}
