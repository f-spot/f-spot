//
// QueuePipeline.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Hyena.Collections
{
    public class QueuePipeline <T> where T : class
    {
        #pragma warning disable 0067
        // FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
        public event EventHandler Finished;
        #pragma warning restore 0067

        private object sync = new object ();

        private QueuePipelineElement<T> first_element;
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

        private void OnElementFinished (object o, EventArgs args)
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
            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
