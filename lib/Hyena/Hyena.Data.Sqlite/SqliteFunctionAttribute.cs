//
// Derived from
// Mono.Data.Sqlite.SQLiteFunctionAttribute.cs
//
// Author(s):
//   Robert Simpson (robert@blackcastlesoft.com)
//
// Adapted and modified for the Mono Project by
//   Marek Habersack (grendello@gmail.com)
//
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// Copyright (C) 2007 Marek Habersack
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/********************************************************
 * ADO.NET 2.0 Data Provider for Sqlite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;

namespace Hyena.Data.Sqlite
{
	/// <summary>
	/// A simple custom attribute to enable us to easily find user-defined functions in
	/// the loaded assemblies and initialize them in Sqlite as connections are made.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class SqliteFunctionAttribute : Attribute
	{
		internal Type _instanceType;

		/// <summary>
		/// Default constructor, initializes the internal variables for the function.
		/// </summary>
		public SqliteFunctionAttribute ()
		{
			Name = "";
			Arguments = -1;
			FuncType = FunctionType.Scalar;
		}

		/// <summary>
		/// The function's name as it will be used in Sqlite command text.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The number of arguments this function expects.  -1 if the number of arguments is variable.
		/// </summary>
		public int Arguments { get; set; }

		/// <summary>
		/// The type of function this implementation will be.
		/// </summary>
		public FunctionType FuncType { get; set; }
	}
}
