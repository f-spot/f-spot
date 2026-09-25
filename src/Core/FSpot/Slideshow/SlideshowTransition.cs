// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Slideshow
{
	/// <summary>
	/// The slide transitions GTK F-Spot ships as add-ins (FSpot.Transitions.Cover, .Dissolve, .Push).
	/// </summary>
	public enum SlideshowTransitionKind
	{
		Cover,
		Dissolve,
		Push,
	}

	/// <summary>
	/// Where the outgoing and incoming slide sit at one point of a transition. Offsets are fractions of
	/// the view width; each slide is drawn letterboxed on black, as the GTK Cairo transitions do.
	/// </summary>
	public readonly struct SlideshowFrame
	{
		public SlideshowFrame (double previousOffset, double previousOpacity, double nextOffset, double nextOpacity)
		{
			PreviousOffset = previousOffset;
			PreviousOpacity = previousOpacity;
			NextOffset = nextOffset;
			NextOpacity = nextOpacity;
		}

		public double PreviousOffset { get; }
		public double PreviousOpacity { get; }
		public double NextOffset { get; }
		public double NextOpacity { get; }
	}

	public static class SlideshowTransition
	{
		/// <summary>The GTK SlideShow widget flips every 6000 ms.</summary>
		public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds (6000);

		/// <summary>The GTK SlideShow animates each transition over 2 s.</summary>
		public static readonly TimeSpan Duration = TimeSpan.FromSeconds (2);

		/// <summary>Transitions in the order the GTK combo box lists its add-ins; the first is the default.</summary>
		public static IReadOnlyList<SlideshowTransitionKind> All { get; } = new[] {
			SlideshowTransitionKind.Cover,
			SlideshowTransitionKind.Dissolve,
			SlideshowTransitionKind.Push,
		};

		public static double Progress (TimeSpan elapsed)
		{
			if (elapsed <= TimeSpan.Zero)
				return 0;
			if (elapsed >= Duration)
				return 1;
			return elapsed.TotalMilliseconds / Duration.TotalMilliseconds;
		}

		/// <summary>
		/// Mirrors the Cairo drawing in FSpot.Addins.Transitions: Push slides both photos left, Cover slides
		/// the new photo in over the old one, and Dissolve cross-fades the new photo in over the old one.
		/// </summary>
		public static SlideshowFrame Frame (SlideshowTransitionKind kind, double progress)
		{
			double p = Math.Min (1, Math.Max (0, progress));
			return kind switch {
				SlideshowTransitionKind.Push => new SlideshowFrame (-p, 1, 1 - p, 1),
				SlideshowTransitionKind.Cover => new SlideshowFrame (0, 1, 1 - p, 1),
				SlideshowTransitionKind.Dissolve => new SlideshowFrame (0, 1, 0, p),
				_ => new SlideshowFrame (0, 0, 0, 1),
			};
		}
	}
}
