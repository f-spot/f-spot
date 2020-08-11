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
	/// <remarks>
	///   Implements a safe writing pattern by first copying the file to a
	///   temporary location. This temporary file is used for writing. When the
	///   stream is closed, the temporary file is moved to the original
	///   location.
	/// </remarks>
	public sealed class TagLibFileAbstraction : TagLib.File.IFileAbstraction
	{
		FileStream stream;
		SafeUri tmpWriteUri;

		const string TMP_INFIX = ".tmpwrite";

		public string Name {
			get => Uri.ToString ();
		}

		public SafeUri Uri { get; set; }

		public Stream ReadStream {
			get {
				if (stream == null)
					stream = new FileStream (Uri.AbsolutePath, FileMode.Open, FileAccess.Read);

				if (!stream.CanRead)
					throw new Exception ("Can't read from this resource");

				return stream;
			}
		}

		public Stream WriteStream {
			get {
				if (stream == null) {
					var file = Uri.AbsolutePath;
					if (!File.Exists (file))
						CopyToTmp ();

					stream = new FileStream (file, FileMode.OpenOrCreate, FileAccess.Write);
				}

				if (!stream.CanWrite)
					throw new Exception ("Stream still open in reading mode!");

				return stream;
			}
		}

		void CopyToTmp ()
		{
			var srcFile = Uri.AbsolutePath;
			tmpWriteUri = CreateTmpFile ();

			File.Copy (srcFile, tmpWriteUri.AbsolutePath, true);
		}

		void CommitTmp ()
		{
			if (tmpWriteUri == null)
				return;

			var file = Uri.AbsolutePath;
			var tmpFile = tmpWriteUri.AbsolutePath;

			File.Move (tmpFile, file);
		}

		SafeUri CreateTmpFile ()
		{
			var uri = Uri.GetBaseUri ().Append ("." + Uri.GetFilenameWithoutExtension ());
			var tmp_uri = $"{uri}{TMP_INFIX}{Uri.GetExtension ()}";
			return new SafeUri (tmp_uri, true);
		}

		public void CloseStream (Stream stream)
		{
			stream.Close ();
			if (stream == this.stream) {
				if (stream.CanWrite)
					CommitTmp ();

				this.stream = null;
			}
		}
	}
}
