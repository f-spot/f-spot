//
// ElementQueueProcessor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using NUnit.Framework;

namespace Hyena.Collections.Tests
{
	[TestFixture]
	public class QueuePipelineTests
	{
		class FakeElement : QueuePipelineElement<object>
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

		void BuildPipeline (int count)
		{
			var elements = new List<FakeElement> ();
			for (int i = 0; i < count; i++) {
				elements.Add (new FakeElement ());
			}

			var qp = new QueuePipeline<object> ();
			foreach (FakeElement s in elements) {
				qp.AddElement (s);
			}

			Assert.AreEqual (elements[0], qp.FirstElement);

			int index = 0;
			var element = (FakeElement)qp.FirstElement;
			while (element != null) {
				Assert.AreEqual (elements[index++], element);
				element = (FakeElement)element.NextElement;
			}
		}
	}
}
