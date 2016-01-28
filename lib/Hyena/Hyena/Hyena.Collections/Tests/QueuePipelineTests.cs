//
// ElementQueueProcessor.cs
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

#if ENABLE_TESTS

using System;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;

using Hyena.Collections;

namespace Hyena.Collections.Tests
{
    [TestFixture]
    public class QueuePipelineTests
    {
        private class FakeElement : QueuePipelineElement<object>
        {
            protected override object ProcessItem (object item)
            {
                return null;
            }
        }

        [Test]
        public void BuildPipeline ()
        {
            BuildPipeline (1);
            BuildPipeline (2);
            BuildPipeline (3);
            BuildPipeline (10);
            BuildPipeline (1000);
        }

        private void BuildPipeline (int count)
        {
            List<FakeElement> elements = new List<FakeElement> ();
            for (int i = 0; i < count; i++) {
                elements.Add (new FakeElement ());
            }

            QueuePipeline<object> qp = new QueuePipeline<object> ();
            foreach (FakeElement s in elements) {
                qp.AddElement (s);
            }

            Assert.AreEqual (elements[0], qp.FirstElement);

            int index = 0;
            FakeElement element = (FakeElement)qp.FirstElement;
            while (element != null) {
                Assert.AreEqual (elements[index++], element);
                element = (FakeElement)element.NextElement;
            }
        }
    }
}

#endif
