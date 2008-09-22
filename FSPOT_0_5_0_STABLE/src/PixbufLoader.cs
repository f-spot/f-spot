/*
 * PixbufLoader.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 */
using Gdk;
using Gtk;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

using FSpot.Utils;

public class PixbufLoader {

	// Types.

	protected class RequestItem {
		/* The path to the image.  */
		public Uri uri;

		/* Order value; requests with a lower value get performed first.  */
		public int order;

		/* The pixbuf obtained from the operation.  */
		public Pixbuf result;

		/* the maximium size both must be greater than zero if either is */
		public int width;
		public int height;

		public RequestItem (Uri uri, int order, int width, int height) {
			this.uri = uri;
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

	/* A dict of all the requests; note that the current request
	   isn't in the dict.  */
	Dictionary<Uri, RequestItem> requests_by_uri;
//	private Hashtable requests_by_path;

	/* Current request.  Request currently being handled by the
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

	public delegate void PixbufLoadedHandler (PixbufLoader loader, Uri uri, int order, Pixbuf result);
	public event PixbufLoadedHandler OnPixbufLoaded;

	public PixbufLoader ()
	{
		queue = new ArrayList ();
		requests_by_uri = new Dictionary<Uri, RequestItem> ();
//		requests_by_path = Hashtable.Synchronized (new Hashtable ());
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

	public void Request (Uri uri, int order)
	{
		Request (uri, order, 0, 0);
	}

	public virtual void Request (Uri uri, int order, int width, int height)
	{
		lock (queue) {
			if (InsertRequest (uri, order, width, height))
				Monitor.Pulse (queue);
		}
	}

	public void Cancel (Uri uri)
	{
		lock (queue) {
			RequestItem r = requests_by_uri [uri];
			if (r != null) {
				requests_by_uri.Remove (uri);
				queue.Remove (r);
			}
		}
	}

	// Private utility methods.

	protected virtual void ProcessRequest (RequestItem request)
	{
		Pixbuf orig_image;
		try {
			using (FSpot.ImageFile img = FSpot.ImageFile.Create (request.uri)) {
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

	private bool InsertRequest (Uri uri, int order, int width, int height)
	{
		/* Check if this is the same as the request currently being processed.  */
		lock(processed_requests) {
			if (current_request != null && current_request.uri == uri)
				return false;
		}
		/* Check if a request for this path has already been queued.  */
		RequestItem existing_request;
		if (requests_by_uri.TryGetValue (uri, out existing_request)) {
			/* FIXME: At least for now, this shouldn't happen.  */
			if (existing_request.order != order)
				Log.WarningFormat ("BUG: Filing another request of order {0} (previously {1}) for `{2}'",
						   order, existing_request.order, uri);

			queue.Remove (existing_request);
			queue.Add (existing_request);
			return false;
		}

		/* New request, just put it on the queue with the right order.  */
		RequestItem new_request = new RequestItem (uri, order, width, height);

		queue.Add (new_request);

		lock (queue) {
			requests_by_uri.Add (uri, new_request);
		}
		return true;
	}

	/* The worker thread's main function.  */
	private void WorkerThread ()
	{
		try {
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
					requests_by_uri.Remove (current_request.uri);
				}
				
				ProcessRequest (current_request);
			}
		} catch (ThreadAbortException) {
			//Aborting
		}
	}
	
	protected virtual void EmitLoaded (Queue results)
	{
		if (OnPixbufLoaded != null) {
			foreach (RequestItem r in results)
				OnPixbufLoaded (this, r.uri, r.order, r.result);
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
