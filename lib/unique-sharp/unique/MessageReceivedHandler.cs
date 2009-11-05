// MessageReceivedHandler.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// Copyright (c) 2009 Stephane Delcroix
//
// This is open source software. See COPYING for details.
//

namespace Unique {

	using System;

	public delegate void MessageReceivedHandler(object o, MessageReceivedArgs args);

	public class MessageReceivedArgs : GLib.SignalArgs {
		public int Command{
			get {
				return (int) Args[0];
			}
		}

		public Unique.MessageData MessageData{
			get {
				return (Unique.MessageData) Args[1];
			}
		}

		public uint Time{
			get {
				return (uint) Args[2];
			}
		}

	}
}
