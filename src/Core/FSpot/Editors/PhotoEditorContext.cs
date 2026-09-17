// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using TagLib.Image;

namespace FSpot.Editors
{
	public sealed class PhotoEditorContext
	{
		public PixelRect Selection { get; set; }
		public ImageOrientation Orientation { get; set; } = ImageOrientation.TopLeft;
		public int RedEyeThreshold { get; set; } = -15;
		public int ResizeMaxEdge { get; set; }
		public double ColorExposure { get; set; }
		public double ColorBrightness { get; set; }
		public double ColorContrast { get; set; }
		public double ColorHue { get; set; }
		public double ColorSaturation { get; set; }
		public double ColorTemperature { get; set; } = 5000;
		public double ColorTint { get; set; }
		public float BwRed { get; set; } = 80;
		public float BwGreen { get; set; } = 10;
		public float BwBlue { get; set; } = 10;
		public byte[] BwCurve { get; set; }
		public double TiltDegrees { get; set; }
		public double SoftFocusRadius { get; set; } = 0.5;
		public double SharpenAmount { get; set; } = 0.5;
		public double SharpenRadius { get; set; } = 5;
		public double SharpenThreshold { get; set; }
	}
}
