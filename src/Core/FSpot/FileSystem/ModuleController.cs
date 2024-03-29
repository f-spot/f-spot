//
// ModuleController.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using TinyIoC;

namespace FSpot.FileSystem
{
	public static class ModuleController
	{
		public static void RegisterTypes (TinyIoCContainer container)
		{
			container.Register<IFileSystem, DotNetFileSystem> ().AsSingleton ();
			container.Register<IEnvironment, EnvironmentAdapter> ().AsSingleton ();
		}
	}
}
