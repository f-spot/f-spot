//
// Derived from
// Mono.Data.Sqlite.SQLiteFunction.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Data.Sqlite
{
	using System;

	/// <summary>
	/// The type of user-defined function to declare
	/// </summary>
	public enum FunctionType
	{
		/// <summary>
		/// Scalar functions are designed to be called and return a result immediately.  Examples include ABS(), Upper(), Lower(), etc.
		/// </summary>
		Scalar = 0,
		/// <summary>
		/// Aggregate functions are designed to accumulate data until the end of a call and then return a result gleaned from the accumulated data.
		/// Examples include SUM(), COUNT(), AVG(), etc.
		/// </summary>
		Aggregate = 1,
		/// <summary>
		/// Collation sequences are used to sort textual data in a custom manner, and appear in an ORDER BY clause.  Typically text in an ORDER BY is
		/// sorted using a straight case-insensitive comparison function.  Custom collating sequences can be used to alter the behavior of text sorting
		/// in a user-defined manner.
		/// </summary>
		Collation = 2,
	}

	/// <summary>
	/// This abstract class is designed to handle user-defined functions easily.  An instance of the derived class is made for each
	/// connection to the database.
	/// </summary>
	public abstract class SqliteFunction : IDisposable
	{
		/// <summary>
		/// Scalar functions override this method to do their magic.
		/// </summary>
		/// <remarks>
		/// Parameters passed to functions have only an affinity for a certain data type, there is no underlying schema available
		/// to force them into a certain type.  Therefore the only types you will ever see as parameters are
		/// DBNull.Value, Int64, Double, String or byte[] array.
		/// </remarks>
		/// <param name="args">The arguments for the command to process</param>
		/// <returns>You may return most simple types as a return value, null or DBNull.Value to return null, DateTime, or
		/// you may return an Exception-derived class if you wish to return an error to Sqlite.  Do not actually throw the error,
		/// just return it!</returns>
		public virtual object Invoke (object[] args)
		{
			return null;
		}

		/// <summary>
		/// Aggregate functions override this method to do their magic.
		/// </summary>
		public virtual void Step (object[] args, int stepNumber, ref object contextData)
		{
		}

		/// <summary>
		/// Aggregate functions override this method to finish their aggregate processing.
		/// </summary>
		public virtual object Final (object contextData)
		{
			return null;
		}

		/// <summary>
		/// User-defined collation sequences override this method to provide a custom string sorting algorithm.
		/// </summary>
		/// <param name="param1">The first string to compare</param>
		/// <param name="param2">The second strnig to compare</param>
		/// <returns>1 if param1 is greater than param2, 0 if they are equal, or -1 if param1 is less than param2</returns>
		public virtual int Compare (string param1, string param2)
		{
			return 0;
		}

		/// <summary>
		/// Placeholder for a user-defined disposal routine
		/// </summary>
		/// <param name="disposing">True if the object is being disposed explicitly</param>
		protected virtual void Dispose (bool disposing)
		{
		}

		/// <summary>
		/// Disposes of any active contextData variables that were not automatically cleaned up.
		/// </summary>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
	}
}
