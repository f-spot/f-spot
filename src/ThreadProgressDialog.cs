namespace FSpot {

	using Gtk;
	using GtkSharp;
	using System;
	using System.Threading;

	public class ThreadProgressDialog : Dialog {

		private int total;
		private int completed;
		
		uint progress_timer;

		private ProgressBar progress_bar;
		private Label message_label;
		
		private Thread thread;

		private String pending_message;

		public ThreadProgressDialog (Thread thread, int total) {
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
			
			AddButton ("Cancel", ResponseType.Cancel);
			
			Response += new ResponseHandler (HandleResponse);
			Destroyed += new EventHandler (HandleDestroy);
		}
		
		private void HandleResponse (object obj, ResponseArgs args) {
			this.Destroy ();
		}

		private bool HandleUpdate () 
		{
			message_label.Text = pending_message;
			progress_bar.Text = String.Format ("{0} of {1}", completed, total);
			progress_bar.Fraction = (double) completed/total;
			
			if (thread.IsAlive) 
				return true;

			if (progress_timer != 0)
				GLib.Source.Remove (progress_timer);

			progress_timer = 0;
			return false;
		}

		private void HandleDestroy (object sender, EventArgs args)
		{
			if (thread.IsAlive) 
				thread.Abort ();

			HandleUpdate ();
		}
		
		public void Start () {
			ShowAll ();
			thread.Start ();
			progress_timer = GLib.Timeout.Add (75, new GLib.TimeoutHandler (HandleUpdate));
		}

		public void Update (string message) {
			pending_message = message;
			completed++;
		}
	}
}
