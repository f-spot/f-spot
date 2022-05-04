//
// QueuePipeline.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Collections
{
	public class QueuePipeline<T> where T : class
	{
#pragma warning disable 0067
		// FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
		public event EventHandler Finished;
#pragma warning restore 0067

		object sync = new object ();

		QueuePipelineElement<T> first_element;
		internal QueuePipelineElement<T> FirstElement {
			get { return first_element; }
		}

		public QueuePipeline ()
		{
		}

		public void AddElement (QueuePipelineElement<T> element)
		{
			element.Finished += OnElementFinished;
			lock (sync) {
				if (first_element == null) {
					first_element = element;
					return;
				}

				QueuePipelineElement<T> current = first_element;

				while (current != null) {
					if (current.NextElement == null) {
						current.NextElement = element;
						break;
					}

					current = current.NextElement;
				}
			}
		}

		public virtual void Enqueue (T item)
		{
			if (first_element == null) {
				throw new InvalidOperationException ("There are no elements in this pipeline");
			}

			first_element.Enqueue (item);
		}

		public virtual void Cancel ()
		{
			if (first_element != null) {
				first_element.Cancel ();
			}
		}

		void OnElementFinished (object o, EventArgs args)
		{
			bool any_processing = false;

			lock (sync) {
				QueuePipelineElement<T> element = FirstElement;
				while (element != null) {
					any_processing |= element.Processing || element.ProcessedCount < element.TotalCount;
					if (any_processing) {
						break;
					}
					element = element.NextElement;
				}
			}

			if (!any_processing) {
				OnFinished ();
			}
		}

		protected virtual void OnFinished ()
		{
			Finished?.Invoke (this, EventArgs.Empty);
		}
	}
}
