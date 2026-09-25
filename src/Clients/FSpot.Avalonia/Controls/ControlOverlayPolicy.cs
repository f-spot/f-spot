// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Controls;

/// <summary>The GTK ControlOverlay.VisibilityType states.</summary>
public enum ControlOverlayVisibility
{
	None,
	Partial,
	Full,
}

/// <summary>
/// Timings and opacities of the GTK fullscreen ControlOverlay and FullScreenView cursor handling.
/// </summary>
public static class ControlOverlayPolicy
{
	/// <summary>ControlOverlay hides itself 2000 ms after it was last shown, unless the pointer is over it.</summary>
	public static readonly TimeSpan AutoHide = TimeSpan.FromMilliseconds (2000);

	/// <summary>ControlOverlay.Dismiss ignores show requests for 2000 ms.</summary>
	public static readonly TimeSpan DismissGrace = TimeSpan.FromMilliseconds (2000);

	/// <summary>FullScreenView hides the pointer 3000 ms after it last moved.</summary>
	public static readonly TimeSpan CursorHide = TimeSpan.FromMilliseconds (3000);

	/// <summary>The fade steps by 0.04 every 40 ms.</summary>
	public static readonly TimeSpan FadeTick = TimeSpan.FromMilliseconds (40);
	public const double FadeStep = 0.04;

	public static double TargetOpacity (ControlOverlayVisibility visibility)
		=> visibility switch {
			ControlOverlayVisibility.Partial => 0.4,
			ControlOverlayVisibility.Full => 0.8,
			_ => 0.0,
		};

	/// <summary>FullScreenView reveals the controls when the pointer moves into the bottom quarter.</summary>
	public static bool RevealsControls (double pointerY, double height)
		=> height > 0 && pointerY > height * 0.75;

	/// <summary>One 40 ms fade step toward <paramref name="target"/>.</summary>
	public static double StepToward (double opacity, double target)
	{
		if (Math.Abs (target - opacity) < 0.05)
			return target;
		return target > opacity
			? Math.Min (target, opacity + FadeStep)
			: Math.Max (target, opacity - FadeStep);
	}
}
