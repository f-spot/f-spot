using System;
using System.Collections;
using Gtk;
using FSpot.UI.Dialog;

#if !GTK_2_10
namespace FSpot {
	public class PrintDialog {
		[Glade.Widget] private Gtk.Dialog print_dialog;
		
		[Glade.Widget] private Gtk.VBox     preview_vbox;
		[Glade.Widget] private Gnome.Canvas preview_canvas;

		public enum PrintUnitBase {
			Dimentionless = (1 << 0), /* For percentages and like */
			Absolute = (1 << 1),      /* Real world distances - i.e. mm, cm... */
			Device = (1 << 2),        /* Semi-real device-dependent distances i.e. pixels */
			Userspace = (1 << 3)      /* Mathematical coordinates */
		}

		public static string PAPER_SIZE                = "Settings.Output.Media.PhysicalSize"; /* Paper name, such as A4 or Letter */
		public static string PAPER_WIDTH               = "Settings.Output.Media.PhysicalSize.Width"; /* Arbitrary units - use conversion */
		public static string PAPER_HEIGHT              = "Settings.Output.Media.PhysicalSize.Height"; /* Arbitrary units - use conversion */
		public static string PAPER_ORIENTATION         = "Settings.Output.Media.PhysicalOrientation"; /* R0, R90, R180, R270 */
		public static string PAPER_ORIENTATION_MATRIX  = "Settings.Output.Media.PhysicalOrientation.Paper2PrinterTransform"; /* 3x2 abstract matrix */
		
		public static string PAGE_ORIENTATION        = "Settings.Document.Page.LogicalOrientation"; /* R0, R90, R180, R270 */
		public static string PAGE_ORIENTATION_MATRIX = "Settings.Document.Page.LogicalOrientation.Page2LayoutTransform"; /* 3x2 abstract matrix */
		
		/* Just a reminder - application is only interested in logical orientation */
		public static string ORIENTATION = PAGE_ORIENTATION;

		public static string LAYOUT        = "Settings.Document.Page.Layout";        /* Id of layout ('Plain' is always no-special-layout) */
		public static string LAYOUT_WIDTH  = "Settings.Document.Page.Layout.Width";  /* Double value */
		public static string LAYOUT_HEIGHT = "Settings.Document.Page.Layout.Height"; /* Double value */
		
		public static string PAPER_SOURCE = "Settings.Output.PaperSource"; /* String value, like "Tray 1" */
		
		/* Master resolution, i.e. ink dots for color printer RGB resolution is usually smaller */
		public static string RESOLUTION       = "Settings.Output.Resolution";       /* String value, like 300x300 or 300dpi */
		public static string RESOLUTION_DPI   = "Settings.Output.Resolution.DPI";   /* Numeric value, like 300, if meaningful */
		public static string RESOLUTION_DPI_X = "Settings.Output.Resolution.DPI.X"; /* Numeric value */
		public static string RESOLUTION_DPI_Y = "Settings.Output.Resolution.DPI.Y"; /* Numeric value */
		
		/* These belong to 'Output' because PGL may implement multiple copies itself */
		public static string NUM_COPIES               = "Settings.Output.Job.NumCopies"; /* Number of copies */
		public static string NONCOLLATED_COPIES_IN_HW = "Settings.Output.Job.NonCollatedCopiesHW";
		public static string COLLATED_COPIES_IN_HW    = "Settings.Output.Job.CollatedCopiesHW";
		
		public static string COLLATE   = "Settings.Output.Job.Collate";   /* Boolean (true|yes|1 false|no|0) */
		public static string DUPLEX    = "Settings.Output.Job.Duplex";   /* Boolean (true|yes|1 false|no|0) */
		public static string TUMBLE    = "Settings.Output.Job.Tumble";   /* Boolean (true|yes|1 false|no|0) */
		/* String value, like no-hold|indefinite|day-time|evening|night|weekend|second-shift|third-shift*/
		public static string HOLD      = "Settings.Output.Job.Hold";   
		
		/* These are ignored by libgnomeprint, but you may want to get/set/inspect these */
		/* Libgnomeprintui uses these for displaying margin symbols */
		public static string PAGE_MARGIN_LEFT   = "Settings.Document.Page.Margins.Left";   /* Length, i.e. use conversion */
		public static string PAGE_MARGIN_RIGHT  = "Settings.Document.Page.Margins.Right";  /* Length, i.e. use conversion */
		public static string PAGE_MARGIN_TOP    = "Settings.Document.Page.Margins.Top";    /* Length, i.e. use conversion */
		public static string PAGE_MARGIN_BOTTOM = "Settings.Document.Page.Margins.Bottom"; /* Length, i.e. use conversion */
		
		/* These are ignored by libgnomeprint, and you most probably cannot change these too */
		/* Also - these are relative to ACTUAL PAGE IN PRINTER - not physicalpage */
		/* Libgnomeprintui uses these for displaying margin symbols */
		public static string PAPER_MARGIN_LEFT   = "Settings.Output.Media.Margins.Left";   /* Length, i.e. use conversion */
		public static string PAPER_MARGIN_RIGHT  = "Settings.Output.Media.Margins.Right";  /* Length, i.e. use conversion */
		public static string PAPER_MARGIN_TOP    = "Settings.Output.Media.Margins.Top";    /* Length, i.e. use conversion */
		public static string PAPER_MARGIN_BOTTOM = "Settings.Output.Media.Margins.Bottom"; /* Length, i.e. use conversion */
		
		/* More handy keys */
		public static string OUTPUT_FILENAME = "Settings.Output.Job.FileName"; /* Filename used when printing to file. */
		public static string DOCUMENT_NAME   = "Settings.Document.Name"; /* The name of the document 'Cash flow 2002', `Grandma cookie recipies' */
		public static string PREFERED_UNIT   = "Settings.Document.PreferedUnit"; /* Abbreviation for the preferred unit cm, in,... */
		
		private Gnome.PrintJob print_job;
		private Photo [] photos;

		private void Render ()
		{
			double page_width, page_height;
			print_job.GetPageSize (out page_width, out page_height);
			Gnome.PrintContext ctx = print_job.Context;

			ArrayList exceptions = new ArrayList ();

			foreach (Photo photo in photos) {
				Gdk.Pixbuf image =  null;
				try {
					image = FSpot.PhotoLoader.Load (photo);

					if (image == null)
						throw new System.Exception ("Error loading picture");

				} catch (Exception  e) {
					exceptions.Add (e);
					continue;
				}

				Gdk.Pixbuf flat = PixbufUtils.Flatten (image);
				if (flat != null) {
					image.Dispose ();
					image = flat;
				}

				Gnome.Print.Beginpage (ctx, "F-Spot" + photo.DefaultVersionUri.ToString ());				

				bool rotate = false;
				double width = page_width;
				double height = page_height;
				if (image.Width > image.Height) {
					rotate = true;
					width = page_height;
					height = page_width;
				}

				double scale = System.Math.Min (width / image.Width, 
								height / image.Height);

				Gnome.Print.Gsave (ctx);

				if (rotate) {
					Gnome.Print.Rotate (ctx, 90);
					Gnome.Print.Translate (ctx, 0, -page_width);
				}
				
				Gnome.Print.Translate (ctx,
						       (width - image.Width * scale) / 2.0,
						       (height - image.Height * scale) / 2.0);
				
				Gnome.Print.Scale (ctx, image.Width * scale, image.Height * scale);
				Gnome.Print.Pixbuf (ctx, image);
				Gnome.Print.Grestore (ctx);

				Gnome.Print.Showpage (ctx);
				image.Dispose ();
			}
			
			print_job.Close ();

			if (exceptions.Count != 0) {
				//FIXME string freeze the message here is not
				//really appropriate to the problem.
				Dialog md = new EditExceptionDialog (print_dialog, 
								     (Exception [])exceptions.ToArray (typeof (Exception)));
				md.Run ();
				md.Destroy ();
			}
				
		}

		private void HandleConfigureClicked (object sender, System.EventArgs args)
		{
			RunGnomePrintDialog ();
		}
		      
		private void RunGnomePrintDialog ()
		{
			Gnome.PrintDialog gnome_dialog = new Gnome.PrintDialog (print_job, "Print Photos", 0);
			int response = gnome_dialog.Run ();
			
			Render ();

			switch (response) {
			case (int) Gnome.PrintButtons.Print:
				print_job.Print ();
				break;
			case (int) Gnome.PrintButtons.Preview:
				new Gnome.PrintJobPreview (print_job, "Testing").Show ();
				break;
			}
			gnome_dialog.Destroy ();
		}

		public PrintDialog (Photo [] photos)
		{
			this.photos = photos;

#if ENABLE_CUSTOM_PRINT
			Glade.XML xml = new Glade.XML (null, "f-spot.glade", "print_dialog", "f-spot");
			xml.Autoconnect (this);
#endif

			print_job = new Gnome.PrintJob (Gnome.PrintConfig.Default ());

			//Render ();

#if ENABLE_CUSTOM_PRINT
			int response = print_dialog.Run ();
			
			switch (response) {
			case (int) Gtk.ResponseType.Ok:
				print_job.Print ();
				break;
			}
			print_dialog.Destroy ();
#else
			RunGnomePrintDialog ();
#endif
		}
	}
}
#endif
