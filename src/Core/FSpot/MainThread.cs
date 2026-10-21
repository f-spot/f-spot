// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace FSpot
{
	// Catalog events and finished thumbnail requests run on the thread that
	// called Install. The UI installs a post that reaches its dispatcher.
	public static class MainThread
	{
		static Thread main;
		static Action<Action> post;

		public static void Install (Action<Action> post)
		{
			if (post == null)
				throw new ArgumentNullException (nameof (post));

			main = Thread.CurrentThread;
			MainThread.post = post;
			try {
				main.Name = "Main Thread";
			} catch (InvalidOperationException) {
				// Already named.
			}
		}

		public static void Invoke (Action action)
		{
			if (action == null)
				throw new ArgumentNullException (nameof (action));
			if (main == null)
				throw new InvalidOperationException ("MainThread.Install must be called first");

			if (main == Thread.CurrentThread)
				action ();
			else
				post (action);
		}
	}
}
