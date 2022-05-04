//
// WriteLineElement.cs
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
	public class WriteLineElement<T> : QueuePipelineElement<T> where T : class
	{
		public WriteLineElement ()
		{
			Threaded = false;
		}

		protected override T ProcessItem (T item)
		{
			Console.WriteLine (item);
			return null;
		}
	}
}
