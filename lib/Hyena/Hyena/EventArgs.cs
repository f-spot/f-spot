//
// EventArgs.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009-2010 Alexander Kojevnikov
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena
{
	public class EventArgs<T> : EventArgs
	{
		readonly T value;

		public EventArgs (T value)
		{
			this.value = value;
		}

		public T Value {
			get { return value; }
		}
	}

	public static class EventExtensions
	{
		public static void SafeInvoke<T> (this T @event, params object[] args) where T : class
		{
			var multicast = @event as MulticastDelegate;
			if (multicast != null) {
				foreach (var handler in multicast.GetInvocationList ()) {
					try {
						handler.DynamicInvoke (args);
					} catch (Exception e) {
						Log.Exception (e);
					}
				}
			}
		}
	}
}
