using Gdk;
using Gtk;
using System.Collections;
using System.Threading;
using System;

public class PixbufLoader {

	// Types.

	private class RequestItem {
		/* The path to the image.  */
		public string path;

		/* Order value; requests with a lower value get performed first.  */
		public int order;

		/* The pixbuf obtained from the operation.  */
		public Pixbuf result;

		public RequestItem (string path, int order) {
			this.path = path;
			this.order = order;
		}
	}


	// Private members.

	/* The thread used to handle the requests.  */
	private Thread worker_thread;
	private static ArrayList all_worker_threads = new ArrayList ();

	/* The request queue; it's shared between the threads so it
	   needs to be locked prior to access.  */
	private ArrayList queue;

	/* A hash of all the requests; note that the current request
	   isn't in the hash.  */
	private Hashtable requests_by_path;

	/* Current requeust.  Request currently being handled by the
	   auxiliary thread.  Should be modified only by the auxiliary
	   thread (the GTK thread can only read it).  */
	private RequestItem current_request;

	/* The queue of processed requests.  */
	private Queue processed_requests;

	/* This is used by the helper thread to notify the main GLib
	   thread that there are pending items in the
	   `processed_requests' queue.  */
	ThreadNotify pending_notify;

	/* Whether a notification is pending on `pending_notify'
	   already or not.  */
	private bool pending_notify_notified;


	// Public API.

	public delegate void PixbufLoadedHandler (PixbufLoader loader, string path, int order, Pixbuf result);
	public event PixbufLoadedHandler OnPixbufLoaded;

	public PixbufLoader ()
	{
		queue = new ArrayList ();
		requests_by_path = new Hashtable ();
		processed_requests = new Queue ();

		pending_notify = new ThreadNotify (new Gtk.ReadyEvent (HandleProcessedRequests));

		worker_thread = new Thread (new ThreadStart (WorkerThread));
		worker_thread.Start ();

		all_worker_threads.Add (worker_thread);
	}

	// FIXME?
	static public void Cleanup ()
	{
		foreach (Thread t in all_worker_threads)
			t.Abort ();
	}

	public void Request (string path, int order)
	{
		lock (queue) {
			if (InsertRequest (path, order))
				Monitor.Pulse (queue);
		}
	}

	public void Cancel (string path)
	{
		lock (queue) {
			RequestItem r = requests_by_path [path] as RequestItem;
			if (r != null) {
				requests_by_path.Remove (path);
				queue.Remove (r);
			}
		}
	}

	// Private utility methods.

	private void ProcessRequest (RequestItem request)
	{
		/* Short circuit for JPEG files; use Alex Larsson's fast thumbnail
		   code in that case.  FIXME: Should use gnome-vfs to determine the
		   MIME type rather than just the extension.  */

#if USE_FASTPATH_THUMBS
		if (request.path.ToLower().EndsWith (".jpg") || request.path.ToLower().EndsWith (".jpeg")) {
			Pixbuf scaled_image = JpegUtils.LoadScaled (request.path, size, size);
			
			if (scaled_image != null) {
				request.result = scaled_image;
				return;
			}
			
			/* If this fails, just use Pixbuf.  */
			scaled_image.Dispose ();
		}
#endif

		Pixbuf orig_image;
		try {
			orig_image = new Pixbuf (request.path);
		} catch (GLib.GException ex){
			return;		
		}
		
		if (orig_image == null)
			return;

		request.result = orig_image;
	}

	/* Insert the request in the queue, return TRUE if the queue actually grew.
	   NOTE: Lock the queue before calling.  */
	private bool InsertRequest (string path, int order)
	{
		/* Check if this is the same as the request currently being processed.  */
		if (current_request != null && current_request.path == path)
			return false;

		/* Check if a request for this path has already been queued.  */
		RequestItem existing_request = requests_by_path [path] as RequestItem;
		if (existing_request != null) {
			/* FIXME: At least for now, this shouldn't happen.  */
			if (existing_request.order != order)
				Console.WriteLine ("BUG: Filing another request of order {0} (previously {1}) for `{2}'",
						   order, existing_request.order, path);

			queue.Remove (existing_request);
			queue.Add (existing_request);
			return false;
		}

		/* New request, just put it on the queue with the right order.  */
		RequestItem new_request = new RequestItem (path, order);
#if ORDERED_QUEUE
		if (queue.Count == 0 || (queue [queue.Count - 1] as RequestItem).order <= new_request.order) {
			queue.Add (new_request);
		} else {
			int i = 0;
			foreach (RequestItem r in queue) {
				if (r.order > new_request.order) {
					queue.Insert (i, new_request);
					break;
				}

				i ++;
			}
		}
#else
		queue.Add (new_request);
#endif
		requests_by_path.Add (path, new_request);
		return true;
	}

	/* The worker thread's main function.  */
	private void WorkerThread ()
	{
		while (true) {
			lock (queue) {
				if (current_request != null) {
					processed_requests.Enqueue (current_request);

					if (! pending_notify_notified) {
						pending_notify.WakeupMain ();
						pending_notify_notified = true;
					}

					current_request = null;
				}

				while (queue.Count == 0)
					Monitor.Wait (queue);
				
				int pos = queue.Count - 1;

				current_request = queue [pos] as RequestItem;
				queue.RemoveAt (pos);
				requests_by_path.Remove (current_request.path);
			}

			ProcessRequest (current_request);
		}
	}

	private void HandleProcessedRequests ()
	{
		Queue results;

		lock (queue) {
			/* Copy the queued items out of the shared queue so we hold the lock for
			   as little time as possible.  */
			results = processed_requests.Clone() as Queue;
			processed_requests.Clear ();

			pending_notify_notified = false;
		}

		if (OnPixbufLoaded != null) {
			foreach (RequestItem r in results)
				OnPixbufLoaded (this, r.path, r.order, r.result);
		}
	}
}
