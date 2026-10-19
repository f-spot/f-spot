// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using FSpot;
using FSpot.Settings;

using Hyena;

namespace FSpot.Avalonia.Imaging;

// Open windows share one preference hook. The callbacks run on the UI thread,
// outside this lock, and only when the display profile itself changed.
static class DisplayProfileWatch
{
	static readonly object gate = new ();
	static readonly List<Action> listeners = new ();
	static bool hooked;

	public static void Attach (Action reload)
	{
		ArgumentNullException.ThrowIfNull (reload);
		lock (gate) {
			if (!hooked) {
				Preferences.SettingChanged += OnSettingChanged;
				hooked = true;
			}

			listeners.Add (reload);
		}
	}

	public static void Detach (Action reload)
	{
		lock (gate)
			listeners.Remove (reload);
	}

	static void OnSettingChanged (object? sender, NotifyEventArgs args)
	{
		if (args.Key != Preferences.ColorManagementDisplayProfile)
			return;

		Action[] copy;
		lock (gate)
			copy = listeners.ToArray ();

		if (Dispatcher.UIThread.CheckAccess ())
			Invoke (copy);
		else
			Dispatcher.UIThread.Post (() => Invoke (copy));
	}

	static void Invoke (Action[] copy)
	{
		foreach (var reload in copy) {
			bool stillAttached;
			lock (gate)
				stillAttached = listeners.Contains (reload);
			if (!stillAttached)
				continue;

			try {
				reload ();
			} catch (Exception ex) {
				Logger.Log.Debug (ex, "Failed to refresh a photo after the display profile changed");
			}
		}
	}
}
