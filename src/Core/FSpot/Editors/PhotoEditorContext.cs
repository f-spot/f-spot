// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using FSpot.Imaging;

using TagLib.Image;

namespace FSpot.Editors
{
	// Settings sliders bind straight to these; the Avalonia client re-renders
	// its live preview when one of them raises PropertyChanged.
	public sealed class PhotoEditorContext : INotifyPropertyChanged
	{
		double colorExposure;
		double colorBrightness;
		double colorContrast;
		double colorHue;
		double colorSaturation;
		double colorTemperature = 5000;
		double colorTint;
		float bwRed = 80;
		float bwGreen = 10;
		float bwBlue = 10;
		double tiltDegrees;
		double softFocusRadius = 0.5;

		public event PropertyChangedEventHandler PropertyChanged;

		public PixelRect Selection { get; set; }
		public ImageOrientation Orientation { get; set; } = ImageOrientation.TopLeft;
		public int RedEyeThreshold { get; set; } = -15;
		public int ResizeMaxEdge { get; set; }

		public double ColorExposure {
			get => colorExposure;
			set => Set (ref colorExposure, value);
		}

		public double ColorBrightness {
			get => colorBrightness;
			set => Set (ref colorBrightness, value);
		}

		public double ColorContrast {
			get => colorContrast;
			set => Set (ref colorContrast, value);
		}

		public double ColorHue {
			get => colorHue;
			set => Set (ref colorHue, value);
		}

		public double ColorSaturation {
			get => colorSaturation;
			set => Set (ref colorSaturation, value);
		}

		public double ColorTemperature {
			get => colorTemperature;
			set => Set (ref colorTemperature, value);
		}

		public double ColorTint {
			get => colorTint;
			set => Set (ref colorTint, value);
		}

		public float BwRed {
			get => bwRed;
			set => Set (ref bwRed, value);
		}

		public float BwGreen {
			get => bwGreen;
			set => Set (ref bwGreen, value);
		}

		public float BwBlue {
			get => bwBlue;
			set => Set (ref bwBlue, value);
		}

		public byte[] BwCurve { get; set; }

		public double TiltDegrees {
			get => tiltDegrees;
			set => Set (ref tiltDegrees, value);
		}

		public double SoftFocusRadius {
			get => softFocusRadius;
			set => Set (ref softFocusRadius, value);
		}

		public double SharpenAmount { get; set; } = 0.5;
		public double SharpenRadius { get; set; } = 5;
		public double SharpenThreshold { get; set; }

		void Set<T> (ref T field, T value, [CallerMemberName] string name = null)
		{
			if (EqualityComparer<T>.Default.Equals (field, value))
				return;
			field = value;
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
		}
	}
}
