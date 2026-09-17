//
// AvaloniaThumbnailLoader.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Avalonia.Media.Imaging;

using FSpot.Avalonia.Imaging;

namespace FSpot.Avalonia.Services;

sealed class AvaloniaThumbnailLoader : IPhotoThumbnailLoader
{
	public async Task<Bitmap?> LoadAsync (string path, int size, CancellationToken cancellationToken)
	{
		if (!File.Exists (path))
			return null;

		return await Task.Run (() => AvaloniaPhotoDecoder.DecodeFile (path, size), cancellationToken);
	}
}
