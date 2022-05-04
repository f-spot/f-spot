//
// Buffer.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

namespace Hyena.Downloader
{
	public class Buffer
	{
		public int Length { get; set; }
		public DateTime TimeStamp { get; set; }
		public byte[] Data { get; set; }
	}
}

