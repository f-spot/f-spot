//
// BchswAbstractProfile.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using lcmsNET;

namespace FSpot.Cms
{
	// A port of f_cmsCreateBCHSWabstractProfile from lib/libfspot/f-screen-utils.c:
	// F-Spot's fork of lcms's cmsCreateBCHSWabstractProfile, which adds an exposure
	// factor and an optional tone-curve stage ahead of the CLUT. Little-CMS's own
	// seven-parameter version is not a substitute.
	//
	// The CLUT is filled here rather than through cmsStageSampleCLut16bit because the
	// lcmsNET 1.2.1 binary declares the sampler's output array without [Out], so
	// whatever a managed sampler writes never reaches the table: the call reports
	// success and leaves every node zero. Handing lcms a table we filled ourselves
	// sidesteps the callback entirely; FillTable walks the nodes in the same order
	// cmsStageSampleCLut16bit does, so the result is the same table.
	static class BchswAbstractProfile
	{
		internal struct Adjustments
		{
			public double Exposure;
			public double Brightness;
			public double Contrast;
			public double Hue;
			public double Saturation;
			public CIEXYZ SourceWhitePoint;
			public CIEXYZ DestinationWhitePoint;
		}

		const int Channels = 3;

		public static lcmsNET.Profile Create (int gridPoints, double exposure, double brightness, double contrast,
			double hue, double saturation, CIExyY sourceWhitePoint, CIExyY destinationWhitePoint,
			lcmsNET.ToneCurve[] curves)
		{
			if (gridPoints < 2)
				throw new ArgumentOutOfRangeException (nameof (gridPoints), gridPoints, "a CLUT needs at least two grid points per axis");
			if (curves != null && curves.Length != Channels)
				throw new ArgumentException ($"expected {Channels} tone curves, got {curves.Length}", nameof (curves));

			var adjustments = new Adjustments {
				Exposure = exposure,
				Brightness = brightness,
				Contrast = contrast,
				Hue = hue,
				Saturation = saturation,
				SourceWhitePoint = Colorimetric.xyY2XYZ (sourceWhitePoint),
				DestinationWhitePoint = Colorimetric.xyY2XYZ (destinationWhitePoint),
			};

			var profile = lcmsNET.Profile.CreatePlaceholder ();
			try {
				profile.DeviceClass = ProfileClassSignature.Abstract;
				profile.ColorSpace = ColorSpaceSignature.LabData;
				profile.PCS = ColorSpaceSignature.LabData;
				profile.HeaderRenderingIntent = lcmsNET.Intent.Perceptual;

				using (var pipeline = Pipeline.Create (null, Channels, Channels)) {
					// The C code appends the curves before the CLUT, so they act on the
					// Lab input rather than on the CLUT's output.
					if (curves != null)
						Insert (pipeline, Stage.Create (null, Channels, curves));

					uint n = (uint)gridPoints;
					Insert (pipeline, Stage.Create (null, new[] { n, n, n }, Channels, FillTable (gridPoints, adjustments)));

					WriteText (profile, TagSignature.DeviceMfgDesc, "(f-spot internal)");
					WriteText (profile, TagSignature.ProfileDescription, "f-spot BCHSW abstract profile");
					WriteText (profile, TagSignature.DeviceModelDesc, "BCHSW built-in");
					Write (profile.WriteTag (TagSignature.MediaWhitePoint, CIEXYZ.D50), TagSignature.MediaWhitePoint);
					Write (profile.WriteTag (TagSignature.AToB0, pipeline), TagSignature.AToB0);
				}

				return profile;
			} catch {
				profile.Dispose ();
				throw;
			}
		}

		// The node order is the one cmsStageSampleCLut16bit iterates: the last input
		// channel varies fastest, and each node holds its outputChannels values
		// consecutively, so entry (l, a, b) of output channel c sits at
		// ((l * n + a) * n + b) * 3 + c.
		internal static ushort[] FillTable (int gridPoints, Adjustments adjustments)
		{
			checked {
				var table = new ushort[gridPoints * gridPoints * gridPoints * Channels];
				var input = new ushort[Channels];
				var output = new ushort[Channels];
				int index = 0;

				for (int l = 0; l < gridPoints; l++) {
					input[0] = Quantize (l, gridPoints);
					for (int a = 0; a < gridPoints; a++) {
						input[1] = Quantize (a, gridPoints);
						for (int b = 0; b < gridPoints; b++) {
							input[2] = Quantize (b, gridPoints);
							Sample (input, output, adjustments);
							table[index++] = output[0];
							table[index++] = output[1];
							table[index++] = output[2];
						}
					}
				}

				return table;
			}
		}

		// _cmsQuantizeVal: the node's position mapped onto the 16-bit encoding.
		static ushort Quantize (int node, int gridPoints)
		{
			return (ushort)Math.Floor (node * 65535.0 / (gridPoints - 1) + 0.5);
		}

		// bchswSampler, statement for statement.
		internal static void Sample (ushort[] input, ushort[] output, Adjustments bchsw)
		{
			CIELab labIn = Colorimetric.LabEncoded2Float (input);

			// Move the white point in Lab.
			CIEXYZ xyz = Colorimetric.Lab2XYZ (bchsw.SourceWhitePoint, labIn);
			labIn = Colorimetric.XYZ2Lab (bchsw.DestinationWhitePoint, xyz);

			bool shift = labIn.L > 0.5;
			double l = labIn.L / 100;
			if (shift)
				l = 1.0 - l;

			if (l < 0.0)
				l = 0.0;

			double power;
			if (bchsw.Contrast < 0)
				power = 1.0 + bchsw.Contrast;
			else
				power = bchsw.Contrast == 1.0 ? 127 : 1.0 / (1.0 - bchsw.Contrast);

			l = 0.5 * Math.Pow (l * 2.0, power);

			if (shift)
				l = 1.0 - l;

			labIn.L = l * 100;

			CIELCh lchIn = Colorimetric.Lab2LCh (labIn);

			var lchOut = new CIELCh {
				L = lchIn.L * bchsw.Exposure + bchsw.Brightness,
				C = Math.Max (0, lchIn.C + bchsw.Saturation),
				h = lchIn.h + bchsw.Hue,
			};

			CIELab labOut = Colorimetric.LCh2Lab (lchOut);

			ushort[] encoded = Colorimetric.Float2LabEncoded (labOut);
			output[0] = encoded[0];
			output[1] = encoded[1];
			output[2] = encoded[2];
		}

		static void Insert (Pipeline pipeline, Stage stage)
		{
			// On success the pipeline owns the stage; on failure it is still ours.
			if (!pipeline.Insert (stage, StageLoc.At_End)) {
				stage.Dispose ();
				throw new CmsException ("Unable to build the BCHSW pipeline");
			}
		}

		static void WriteText (lcmsNET.Profile profile, TagSignature tag, string text)
		{
			// The C code wrote raw char* here, which lcms2 rejects as a malformed
			// description; a multi-localised string is what the tag actually holds.
			using var mlu = MultiLocalizedUnicode.Create (null);
			if (!mlu.SetASCII ("en", "US", text))
				throw new CmsException ($"Unable to set the text of tag {tag}");
			Write (profile.WriteTag (tag, mlu), tag);
		}

		static void Write (bool written, TagSignature tag)
		{
			if (!written)
				throw new CmsException ($"Unable to write tag {tag} to the BCHSW profile");
		}
	}
}
