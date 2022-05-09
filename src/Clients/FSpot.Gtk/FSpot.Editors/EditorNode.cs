//  EditorNode.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2017 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Mono.Addins;

namespace FSpot.Editors
{
	// TODO: Move EditorNode to FSpot.Extionsions?
	[ExtensionNode ("Editor")]
	public class EditorNode : ExtensionNode
	{
		[NodeAttribute (Required = true)]
		protected string EditorType;

		public Editor GetEditor ()
		{
			return (Editor)Addin.CreateInstance (EditorType);
		}
	}
}
