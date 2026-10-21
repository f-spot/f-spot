// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using FSpot;

namespace FSpot.Avalonia;

public static class AvaloniaMainThread
{
	public static void Install (Action<Action> post)
		=> MainThread.Install (post);

	public static void InstallOnDispatcher ()
		=> Install (action => Dispatcher.UIThread.Post (action));
}
