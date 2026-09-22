// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

using Svg.Skia;

namespace RasterizeIcons;

public static class IconRasterizer
{
	public static void WritePng (IconRasterJob job)
	{
		Directory.CreateDirectory (Path.GetDirectoryName (job.PngPath)!);
		using var svg = SKSvg.CreateFromFile (job.SvgPath);
		var picture = svg.Picture ?? throw new InvalidOperationException ("Failed to rasterize " + job.SvgPath);
		var bounds = picture.CullRect;
		float longest = Math.Max (bounds.Width, bounds.Height);
		if (longest <= 0)
			throw new InvalidOperationException ("SVG has no drawable size: " + job.SvgPath);

		float scale = job.Size / longest;
		int width = Math.Max (1, (int)Math.Round (bounds.Width * scale));
		int height = Math.Max (1, (int)Math.Round (bounds.Height * scale));
		var info = new SKImageInfo (job.Size, job.Size, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create (info)
			?? throw new InvalidOperationException ("Failed to create surface for " + job.SvgPath);
		var canvas = surface.Canvas;
		canvas.Clear (SKColors.Transparent);
		canvas.Translate ((job.Size - width) / 2f, (job.Size - height) / 2f);
		canvas.Scale (scale);
		canvas.Translate (-bounds.Left, -bounds.Top);
		canvas.DrawPicture (picture);
		canvas.Flush ();
		using var image = surface.Snapshot ();
		using var data = image.Encode (SKEncodedImageFormat.Png, 100)
			?? throw new InvalidOperationException ("Failed to encode " + job.PngPath);
		File.WriteAllBytes (job.PngPath, data.ToArray ());
	}
}
