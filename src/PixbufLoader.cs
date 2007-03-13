using Gdk;
using Gtk;
using System.Collections;
using System.Threading;
using System;

public class PixbufLoader {

	// Types.

	protected class RequestItem {
		/* The path to the image.  */
		public string path;

		/* Order value; requests with a lower value get performed first.  */
		public int order;

		/* The pixbuf obtained from the operation.  */
		public Pixbuf result;

		/* the maximium size both must be greater than zero if either is */
		public int width;
		public int height;

		public RequestItem (string path, int order, int width, int height) {
			this.path = path;
			this.order = order;
			this.width = width;
			this.height = height;
			if ((width <= 0 && height > 0) || (height <= 0 && width > 0))
				throw new System.Exception ("Invalid arguments");
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
		requests_by_path = Hashtable.Synchronized (new Hashtable ());
		processed_requests = new Queue ();
		
		pending_notify = new ThreadNotify (new Gtk.ReadyEvent (HandleProcessedRequests));

		worker_thread = new Thread (new ThreadStart (WorkerThread));
		worker_thread.Start ();

		all_worker_threads.Add (worker_thread);
	}

	int block_count;
	public void PushBlock ()
	{
		System.Threading.Interlocked.Increment (ref block_count);
	}

	public void PopBlock ()
	{
		if (System.Threading.Interlocked.Decrement (ref block_count) == 0) {
			lock (queue) { 
				Monitor.Pulse (queue); 
			}
		}
	}

	// FIXME?
	static public void Cleanup ()
	{
		foreach (Thread t in all_worker_threads)
			t.Abort ();
	}

	public void Request (string path, int order)
	{
		Request (path, order, 0, 0);
	}

	public void Request (string path, int order, int width, int height)
	{
		lock (queue) {
			if (InsertRequest (path, order, width, height))
				Monitor.Pulse (queue);
		}
	}

	public void Request (Uri uri, int order)
	{
		Request (uri.ToString (), order);
	}

	public void Request (Uri uri, int order, int width, int height)
	{
		Request (uri.ToString (), order, width, height);
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

	protected virtual void ProcessRequest (RequestItem request)
	{
		Pixbuf orig_image;
		try {
			using (FSpot.ImageFile img = System.IO.File.Exists (request.path) ? FSpot.ImageFile.Create (request.path) : FSpot.ImageFile.Create (new Uri (request.path))) {
				if (request.width > 0) {
					orig_image = img.Load (request.width, request.height);
				} else {
					orig_image = img.Load ();
				}
			}
		} catch (GLib.GException e){
			System.Console.WriteLine (e.ToString ());
			return;		
		}
		
		if (orig_image == null)
			return;
		
		request.result = orig_image;
	}

	/* Insert the request in the queue, return TRUE if the queue actually grew.
	   NOTE: Lock the queue before calling.  */

	private bool InsertRequest (string path, int order, int width, int height)
	{
		/* Check if this is the same as the request currently being processed.  */
		lock(processed_requests) {
			if (current_request != null && current_request.path == path)
				return false;
		}
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
		RequestItem new_request = new RequestItem (path, order, width, height);

		queue.Add (new_request);

		requests_by_path.Add (path, new_request);
		return true;
	}

	/* The worker thread's main function.  */
	private void WorkerThread ()
	{
		while (true) {
			lock (processed_requests) {
				if (current_request != null) {
					processed_requests.Enqueue (current_request);
					
					if (! pending_notify_notified) {
						pending_notify.WakeupMain ();
						pending_notify_notified = true;
					}
					
					current_request = null;
				}
			}

			lock (queue) {
				
				while (queue.Count == 0 || block_count > 0)
					Monitor.Wait (queue);
				
				int pos = queue.Count - 1;

				current_request = queue [pos] as RequestItem;
				queue.RemoveAt (pos);
				requests_by_path.Remove (current_request.path);
			}
			
			ProcessRequest (current_request);
		}
	}
	
	protected virtual void EmitLoaded (Queue results)
	{
		if (OnPixbufLoaded != null) {
			foreach (RequestItem r in results)
				OnPixbufLoaded (this, r.path, r.order, r.result);
		}
	}

	private void HandleProcessedRequests ()
	{
		Queue results;
		
		
		lock (processed_requests) {
			/* Copy the queued items out of the shared queue so we hold the lock for
			   as little time as possible.  */
			results = processed_requests.Clone() as Queue;
			processed_requests.Clear ();

			pending_notify_notified = false;
		}
		
		EmitLoaded (results);
	}
}
