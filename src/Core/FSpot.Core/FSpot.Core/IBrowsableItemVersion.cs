/*
 * IBrowsableItemVersion.cs
 *
 * Author(s):
 *  Ruben Vermeersch <ruben@savanne.be>
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using Hyena;


namespace FSpot.Core
{

	public interface IBrowsableItemVersion : ILoadable
	{

#region Metadata

		/// <summary>
		///   The name of the version. e.g. "Convert to Black and White"
		/// </summary>
		/// <remarks>
		///   This is not the name of the file.
		/// </remarks>
		string Name { get; }

		// TODO: add Comment
		bool IsProtected { get; }

		// TODO: add more metadata

#endregion


#region File Information

		// TODO: BaseUri and Filename are just in the database scheme. Does it make sense to provide them
		//       to the outside?

		/// <summary>
		///   The base uri of the directory of this version. That is the whole uri without the
		///   filename.
		/// </summary>
		SafeUri BaseUri { get; }

		/// <summary>
		///    The filename of this version.
		/// </summary>
		string Filename { get; }

		// TODO: add Comment
		// TODO: not every item is also imported. So does it make sense to have that checksum here?
		//       (If a comment is added, include the easons for having this here!)
		string ImportMD5 { get; }

#endregion

	}
}
