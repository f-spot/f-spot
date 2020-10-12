//
// IImageFileFactory.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

namespace FSpot.Imaging
{
	public interface IImageFileFactory
	{
		IImageFile Create (SafeUri uri);
		bool HasLoader (SafeUri uri);

		bool IsJpeg (SafeUri uri);
		bool IsRaw (SafeUri uri);
		bool IsJpegRawPair (SafeUri file1, SafeUri file2);
	}
}
