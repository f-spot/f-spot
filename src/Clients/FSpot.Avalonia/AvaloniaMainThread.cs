// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using Hyena;

namespace FSpot.Avalonia;

public static class AvaloniaMainThread
{
	public static void Install (Action<InvokeHandler> post)
	{
		if (post == null)
			throw new ArgumentNullException (nameof (post));
		ThreadAssist.InitializeMainThread ();
		ThreadAssist.ProxyToMainHandler = post;
	}

	public static void InstallOnDispatcher ()
		=> Install (handler => Dispatcher.UIThread.Post (() => handler ()));
}
