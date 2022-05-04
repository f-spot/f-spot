//
// QueuePipelineElement.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Hyena.Collections
{
	class ElementProcessCanceledException : ApplicationException
	{
	}

	public abstract class QueuePipelineElement<T> where T : class
	{
#pragma warning disable 0067
		// FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
		public event EventHandler Finished;
		public event EventHandler ProcessedItem;
#pragma warning restore 0067

		Queue<T> queue = new Queue<T> ();
		object monitor = new object ();
		AutoResetEvent thread_wait;
		bool processing = false;
		bool threaded = true;
		bool canceled = false;

		int processed_count;
		public int ProcessedCount {
			get { return processed_count; }
		}

		int total_count;
		public int TotalCount {
			get { return total_count; }
		}

		protected abstract T ProcessItem (T item);

		protected virtual void OnFinished ()
		{
			lock (this) {
				canceled = false;
			}

			lock (queue) {
				total_count = 0;
				processed_count = 0;
			}

			Finished?.Invoke (this, EventArgs.Empty);
		}

		protected void OnProcessedItem ()
		{
			ProcessedItem?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnCanceled ()
		{
			lock (queue) {
				queue.Clear ();
				total_count = 0;
				processed_count = 0;
			}
		}

		public virtual void Enqueue (T item)
		{
			lock (this) {
				lock (queue) {
					queue.Enqueue (item);
					total_count++;
				}

				if (!threaded) {
					Processor (null);
					return;
				}

				if (thread_wait == null) {
					thread_wait = new AutoResetEvent (false);
				}

				if (Monitor.TryEnter (monitor)) {
					Monitor.Exit (monitor);
					ThreadPool.QueueUserWorkItem (Processor);
					thread_wait.WaitOne ();
				}
			}
		}

		protected virtual void EnqueueDownstream (T item)
		{
			if (NextElement != null && item != null) {
				NextElement.Enqueue (item);
			}
		}

		void Processor (object state)
		{
			lock (monitor) {
				if (threaded) {
					thread_wait.Set ();
				}

				lock (this) {
					processing = true;
				}

				try {
					while (queue.Count > 0) {
						CheckForCanceled ();

						T item = null;
						lock (queue) {
							item = queue.Dequeue ();
							processed_count++;
						}

						EnqueueDownstream (ProcessItem (item));
						OnProcessedItem ();
					}
				} catch (ElementProcessCanceledException) {
					OnCanceled ();
				}

				lock (this) {
					processing = false;
				}

				if (threaded) {
					thread_wait.Close ();
					thread_wait = null;
				}

				OnFinished ();
			}
		}

		protected virtual void CheckForCanceled ()
		{
			lock (this) {
				if (canceled) {
					throw new ElementProcessCanceledException ();
				}
			}
		}

		public void Cancel ()
		{
			lock (this) {
				if (processing) {
					canceled = true;
				}

				if (NextElement != null) {
					NextElement.Cancel ();
				}
			}
		}

		public bool Processing {
			get { lock (this) { return processing; } }
		}

		public bool Threaded {
			get { return threaded; }
			set {
				if (processing) {
					throw new InvalidOperationException ("Cannot change threading model while the element is processing");
				}

				threaded = value;
			}
		}

		protected Queue<T> Queue {
			get { return queue; }
		}

		QueuePipelineElement<T> next_element;
		internal QueuePipelineElement<T> NextElement {
			get { return next_element; }
			set { next_element = value; }
		}
	}
}
