namespace FSpot {
	using Gtk;
	using GtkSharp;
	using System;
	
	public class ProgressItem {
		public ProgressItem () {
			
		}
		
		public delegate void ChangedHandler (ProgressItem item);
		public event ChangedHandler Changed;

		double value;
		public double Value {
			get {
				lock (this) {
					return value;
				}
			}
			set {
				lock (this) {
					this.value = value;
					if (Changed != null)
						Changed (this);
				}
			}
		}
	}

	public class ThreadProgressDialog : Dialog {

		private int total;
		private int completed;
		
		FSpot.Delay delay;

		private Gtk.ProgressBar progress_bar;
		private Gtk.Label message_label;
		private Gtk.Button button;
		private System.Threading.Thread thread;

		public ThreadProgressDialog (System.Threading.Thread thread, int total) {
			/*
			if (parent_window)
				this.TransientFor = parent_window;

			*/
			this.Title = thread.Name;
			this.thread = thread;
			
			this.total = total;

			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);
			
			message_label = new Label ("");
			VBox.PackStart (message_label, true, true, 12);
			
			progress_bar = new ProgressBar ();
			VBox.PackStart (progress_bar, true, true, 6);

			button_label = Gtk.Stock.Cancel;
			button = (Gtk.Button) AddButton (button_label, (int)ResponseType.Cancel);
			
			delay = new Delay (new GLib.IdleHandler (HandleUpdate));

			Response += new ResponseHandler (HandleResponse);
			Destroyed += new EventHandler (HandleDestroy);
		}

		private string progress_text;
		public string ProgressText {
			get {
				return progress_text;
			}
			set {
				lock (this) {
					progress_text = value;
					delay.Start ();
				}
			}
		}

		private string button_label;
		public string ButtonLabel {
			get {
				return button_label;
			}			
			set {
				lock (this) {
					button_label = value;
					delay.Start ();
				}
			}
		}
		
		private string message;
		public string Message {
			get {
				return message;
			}
			set {
				lock (this) {
					message = value;
					delay.Start ();
				}
			}
		}
		
		private double fraction;
		public double Fraction {
			get {
				return Fraction;
			}
			set {
				lock (this) {
					fraction = value;
					delay.Start ();
				}
			}
		}

		private void HandleResponse (object obj, ResponseArgs args) {
			this.Destroy ();
		}

		private bool HandleUpdate () 
		{
			message_label.Text = message;
			progress_bar.Text = progress_text;
			progress_bar.Fraction = (double) fraction;
			button.Label = button_label;

			return false;
		}

		private void HandleDestroy (object sender, EventArgs args)
		{
			if (thread.IsAlive) 
				thread.Abort ();

			delay.Stop ();
		}
		
		public void Start () {
			ShowAll ();
			thread.Start ();
		}
	}
}
