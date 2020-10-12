//
// ModuleController.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Utils;

namespace FSpot.Thumbnail
{
	public static class ModuleController
	{
		public static void RegisterTypes (TinyIoCContainer container)
		{
			container.Register<IXdgDirectoryService, XdgDirectoryService> ().AsSingleton ();
			container.Register<IThumbnailService, ThumbnailService> ().AsSingleton ();
			container.Register<IThumbnailerFactory, ThumbnailerFactory> ().AsSingleton ();
			container.Register<IThumbnailLoader, ThumbnailLoader> ().AsSingleton ();
		}
	}
}
