//
// TagLibFileAbstraction.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

namespace FSpot.Utils
{
	/// <summary>
	///   Wraps System.IO into a TagLib IFileAbstraction.
	/// </summary>
	public sealed class TagLibFileAbstraction : TagLib.File.IFileAbstraction
	{
		FileStream stream;

		public string Name {
			get => Uri.ToString ();
		}

		public SafeUri Uri { get; }

		public Stream ReadStream {
			get {
				if (stream == null)
					stream = new FileStream (Uri.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

				if (!stream.CanRead)
					throw new Exception ("Can't read from this resource");

				return stream;
			}
		}

		public Stream WriteStream {
			get {
				if (stream == null) {
					string file = Uri.AbsolutePath;
					string directory = Path.GetDirectoryName (file);
					if (!string.IsNullOrEmpty (directory))
						Directory.CreateDirectory (directory);

					stream = new FileStream (file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
				}

				if (!stream.CanWrite)
					throw new Exception ("Stream still open in reading mode!");

				return stream;
			}
		}

		public TagLibFileAbstraction (SafeUri uri)
		{
			Uri = uri;
		}

		public void CloseStream (Stream stream)
		{
			stream?.Close ();
			if (stream == this.stream)
				this.stream = null;
		}
	}
}
