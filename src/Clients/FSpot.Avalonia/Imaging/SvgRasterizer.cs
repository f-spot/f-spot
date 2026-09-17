//
// SvgRasterizer.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using SkiaSharp;

using Svg.Skia;

namespace FSpot.Avalonia.Imaging;

static class SvgRasterizer
{
	internal const int MaxSvgBytes = 32 * 1024 * 1024;
	internal static int SvgByteLimit = MaxSvgBytes;

	const int SniffLength = 4096;

	static readonly Regex ExternalHref = new (
		@"(href\s*=\s*)(['""])(?:https?:|file:|//)[^'""]*\2",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);
	static readonly Regex ExternalCssUrl = new (
		@"url\(\s*(['""]?)(?:https?:|file:|//)[^)'""]*\1\s*\)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	static SvgRasterizer ()
	{
		Svg.SvgDocument.ResolveExternalImages = Svg.ExternalType.None;
		Svg.SvgDocument.ResolveExternalElements = Svg.ExternalType.None;
		Svg.SvgDocument.ResolveExternalXmlEntites = Svg.ExternalType.None;
	}

	internal static bool IsSvg (string? path, Stream? stream)
	{
		if (HasSvgExtension (path))
			return true;
		if (path is not null)
			return false;
		if (stream is null || !stream.CanSeek)
			return false;
		return SniffSvg (stream);
	}

	internal static byte[] RasterizePng (Stream stream, int maxEdge, uint backgroundArgb = 0)
	{
		if (maxEdge <= 0)
			throw new ArgumentOutOfRangeException (nameof (maxEdge));

		Stream input = stream;
		GZipStream? gzip = null;
		try {
			if (IsGzip (stream)) {
				if (stream.CanSeek)
					stream.Position = 0;
				gzip = new GZipStream (stream, CompressionMode.Decompress, leaveOpen: true);
				input = gzip;
			} else if (stream.CanSeek) {
				stream.Position = 0;
			}

			using var sanitized = Sanitize (input);
			using var svg = SKSvg.CreateFromStream (sanitized);
			var picture = svg.Picture ?? throw new InvalidOperationException ("Failed to rasterize SVG.");
			var bounds = picture.CullRect;
			float longest = Math.Max (bounds.Width, bounds.Height);
			if (longest <= 0)
				throw new InvalidOperationException ("SVG has no drawable size.");

			float scale = maxEdge / longest;
			int width = Math.Max (1, (int)Math.Round (bounds.Width * scale));
			int height = Math.Max (1, (int)Math.Round (bounds.Height * scale));

			var info = new SKImageInfo (width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var surface = SKSurface.Create (info)
				?? throw new InvalidOperationException ("Failed to create SVG surface.");
			var canvas = surface.Canvas;
			canvas.Clear (new SKColor (backgroundArgb));
			canvas.Scale (scale);
			canvas.Translate (-bounds.Left, -bounds.Top);
			canvas.DrawPicture (picture);
			canvas.Flush ();

			using var image = surface.Snapshot ();
			using var data = image.Encode (SKEncodedImageFormat.Png, 100)
				?? throw new InvalidOperationException ("Failed to encode SVG.");
			return data.ToArray ();
		} finally {
			gzip?.Dispose ();
		}
	}

	static bool HasSvgExtension (string? path)
	{
		if (path is null)
			return false;
		string extension = Path.GetExtension (path);
		return extension.Equals (".svg", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals (".svgz", StringComparison.OrdinalIgnoreCase);
	}

	static bool IsGzip (Stream stream)
	{
		if (!stream.CanSeek || stream.Length - stream.Position < 2)
			return false;
		long pos = stream.Position;
		int b0 = stream.ReadByte ();
		int b1 = stream.ReadByte ();
		stream.Position = pos;
		return b0 == 0x1F && b1 == 0x8B;
	}

	static bool SniffSvg (Stream stream)
	{
		long pos = stream.Position;
		try {
			if (IsGzip (stream)) {
				using var gzip = new GZipStream (stream, CompressionMode.Decompress, leaveOpen: true);
				return LooksLikeSvgMarkup (ReadPrefix (gzip));
			}

			return LooksLikeSvgMarkup (ReadPrefix (stream));
		} finally {
			stream.Position = pos;
		}
	}

	static MemoryStream Sanitize (Stream stream)
	{
		using var bounded = new BoundedReadStream (stream, SvgByteLimit);
		using var copy = new MemoryStream ();
		bounded.CopyTo (copy);
		string text = Encoding.UTF8.GetString (copy.ToArray ());
		text = ExternalHref.Replace (text, "$1$2$2");
		text = ExternalCssUrl.Replace (text, "url()");
		return new MemoryStream (Encoding.UTF8.GetBytes (text));
	}

	static byte[] ReadPrefix (Stream stream)
	{
		var buffer = new byte[SniffLength];
		int read = 0;
		while (read < buffer.Length) {
			int n = stream.Read (buffer, read, buffer.Length - read);
			if (n == 0)
				break;
			read += n;
		}

		if (read == buffer.Length)
			return buffer;
		var exact = new byte[read];
		Buffer.BlockCopy (buffer, 0, exact, 0, read);
		return exact;
	}

	static bool LooksLikeSvgMarkup (byte[] prefix)
	{
		if (prefix.Length >= 2 && prefix[0] == 0xFF && prefix[1] == 0xD8)
			return false;
		if (prefix.Length >= 4 && prefix[0] == 0x89 && prefix[1] == (byte)'P' && prefix[2] == (byte)'N' && prefix[3] == (byte)'G')
			return false;

		string text = Encoding.UTF8.GetString (prefix);
		if (text.StartsWith ('\uFEFF'))
			text = text[1..];
		text = text.TrimStart ();
		return text.StartsWith ("<svg", StringComparison.OrdinalIgnoreCase)
			|| (text.StartsWith ("<?xml", StringComparison.OrdinalIgnoreCase)
				&& text.Contains ("<svg", StringComparison.OrdinalIgnoreCase));
	}

	sealed class BoundedReadStream : Stream
	{
		readonly Stream inner;
		readonly int max;
		int total;

		public BoundedReadStream (Stream inner, int max)
		{
			this.inner = inner;
			this.max = max;
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException ();
		public override long Position {
			get => throw new NotSupportedException ();
			set => throw new NotSupportedException ();
		}

		public override void Flush () { }
		public override long Seek (long offset, SeekOrigin origin) => throw new NotSupportedException ();
		public override void SetLength (long value) => throw new NotSupportedException ();
		public override void Write (byte[] buffer, int offset, int count) => throw new NotSupportedException ();

		public override int Read (byte[] buffer, int offset, int count)
		{
			int n = inner.Read (buffer, offset, count);
			Count (n);
			return n;
		}

		public override int Read (Span<byte> buffer)
		{
			int n = inner.Read (buffer);
			Count (n);
			return n;
		}

		void Count (int n)
		{
			if (n <= 0)
				return;
			total += n;
			if (total > max)
				throw new InvalidOperationException ($"SVG exceeds {max} bytes.");
		}
	}
}
