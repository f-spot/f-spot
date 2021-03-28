//
// DbException.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Database
{
	public class DbException : ApplicationException
	{
		public DbException ()
		{
		}

		public DbException (string msg) : base (msg)
		{
		}

		public DbException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
