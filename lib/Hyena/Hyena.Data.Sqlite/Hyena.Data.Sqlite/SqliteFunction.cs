//
// Derived from
// Mono.Data.Sqlite.SQLiteFunction.cs
//
// Author(s):
//   Robert Simpson (robert@blackcastlesoft.com)
//
// Adapted and modified for the Mono Project by
//   Marek Habersack (grendello@gmail.com)
//
// Adapted and modified for the Hyena by
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006,2010 Novell, Inc (http://www.novell.com)
// Copyright (C) 2007 Marek Habersack
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/********************************************************
 * ADO.NET 2.0 Data Provider for Sqlite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/
namespace Hyena.Data.Sqlite
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;
  using System.Globalization;

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
  /// An internal callback delegate declaration.
  /// </summary>
  /// <param name="context">Raw context pointer for the user function</param>
  /// <param name="nArgs">Count of arguments to the function</param>
  /// <param name="argsptr">A pointer to the array of argument pointers</param>
  internal delegate void SqliteCallback(IntPtr context, int nArgs, IntPtr argsptr);
  /// <summary>
  /// An internal callback delegate declaration.
  /// </summary>
  /// <param name="context">Raw context pointer for the user function</param>
  internal delegate void SqliteFinalCallback(IntPtr context);
  /// <summary>
  /// Internal callback delegate for implementing collation sequences
  /// </summary>
  /// <param name="len1">Length of the string pv1</param>
  /// <param name="pv1">Pointer to the first string to compare</param>
  /// <param name="len2">Length of the string pv2</param>
  /// <param name="pv2">Pointer to the second string to compare</param>
  /// <returns>Returns -1 if the first string is less than the second.  0 if they are equal, or 1 if the first string is greater
  /// than the second.</returns>
  internal delegate int SqliteCollation(int len1, IntPtr pv1, int len2, IntPtr pv2);

  /// <summary>
  /// This abstract class is designed to handle user-defined functions easily.  An instance of the derived class is made for each
  /// connection to the database.
  /// </summary>
  /// <remarks>
  /// Although there is one instance of a class derived from SqliteFunction per database connection, the derived class has no access
  /// to the underlying connection.  This is necessary to deter implementers from thinking it would be a good idea to make database
  /// calls during processing.
  ///
  /// It is important to distinguish between a per-connection instance, and a per-SQL statement context.  One instance of this class
  /// services all SQL statements being stepped through on that connection, and there can be many.  One should never store per-statement
  /// information in member variables of user-defined function classes.
  ///
  /// For aggregate functions, always create and store your per-statement data in the contextData object on the 1st step.  This data will
  /// be automatically freed for you (and Dispose() called if the item supports IDisposable) when the statement completes.
  /// </remarks>
  public abstract class SqliteFunction : IDisposable
  {
    /// <summary>
    /// The base connection this function is attached to
    /// </summary>
    /// <summary>
    /// Internal array used to keep track of aggregate function context data
    /// </summary>
    private Dictionary<long, object> _contextDataList;

    /// <summary>
    /// Holds a reference to the callback function for user functions
    /// </summary>
    internal SqliteCallback  _InvokeFunc;
    /// <summary>
    /// Holds a reference to the callbakc function for stepping in an aggregate function
    /// </summary>
    internal SqliteCallback  _StepFunc;
    /// <summary>
    /// Holds a reference to the callback function for finalizing an aggregate function
    /// </summary>
    internal SqliteFinalCallback  _FinalFunc;
    /// <summary>
    /// Holds a reference to the callback function for collation sequences
    /// </summary>
    internal SqliteCollation _CompareFunc;

    /// <summary>
    /// Internal constructor, initializes the function's internal variables.
    /// </summary>
    protected SqliteFunction()
    {
      _contextDataList = new Dictionary<long, object>();
    }

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
    public virtual object Invoke(object[] args)
    {
      return null;
    }

    /// <summary>
    /// Aggregate functions override this method to do their magic.
    /// </summary>
    /// <remarks>
    /// Typically you'll be updating whatever you've placed in the contextData field and returning as quickly as possible.
    /// </remarks>
    /// <param name="args">The arguments for the command to process</param>
    /// <param name="stepNumber">The 1-based step number.  This is incrememted each time the step method is called.</param>
    /// <param name="contextData">A placeholder for implementers to store contextual data pertaining to the current context.</param>
    public virtual void Step(object[] args, int stepNumber, ref object contextData)
    {
    }

    /// <summary>
    /// Aggregate functions override this method to finish their aggregate processing.
    /// </summary>
    /// <remarks>
    /// If you implemented your aggregate function properly,
    /// you've been recording and keeping track of your data in the contextData object provided, and now at this stage you should have
    /// all the information you need in there to figure out what to return.
    /// NOTE:  It is possible to arrive here without receiving a previous call to Step(), in which case the contextData will
    /// be null.  This can happen when no rows were returned.  You can either return null, or 0 or some other custom return value
    /// if that is the case.
    /// </remarks>
    /// <param name="contextData">Your own assigned contextData, provided for you so you can return your final results.</param>
    /// <returns>You may return most simple types as a return value, null or DBNull.Value to return null, DateTime, or
    /// you may return an Exception-derived class if you wish to return an error to Sqlite.  Do not actually throw the error,
    /// just return it!
    /// </returns>
    public virtual object Final(object contextData)
    {
      return null;
    }

    /// <summary>
    /// User-defined collation sequences override this method to provide a custom string sorting algorithm.
    /// </summary>
    /// <param name="param1">The first string to compare</param>
    /// <param name="param2">The second strnig to compare</param>
    /// <returns>1 if param1 is greater than param2, 0 if they are equal, or -1 if param1 is less than param2</returns>
    public virtual int Compare(string param1, string param2)
    {
      return 0;
    }

    /// <summary>
    /// Converts an IntPtr array of context arguments to an object array containing the resolved parameters the pointers point to.
    /// </summary>
    /// <remarks>
    /// Parameters passed to functions have only an affinity for a certain data type, there is no underlying schema available
    /// to force them into a certain type.  Therefore the only types you will ever see as parameters are
    /// DBNull.Value, Int64, Double, String or byte[] array.
    /// </remarks>
    /// <param name="nArgs">The number of arguments</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    /// <returns>An object array of the arguments once they've been converted to .NET values</returns>
    internal object[] ConvertParams(int nArgs, IntPtr argsptr)
    {
      object[] parms = new object[nArgs];
      IntPtr[] argint = new IntPtr[nArgs];
      Marshal.Copy(argsptr, argint, 0, nArgs);

      for (int n = 0; n < nArgs; n++) {
        IntPtr valPtr = (IntPtr)argint[n];
        int type = Native.sqlite3_value_type (valPtr);
        switch (type) {
            case SQLITE_INTEGER:
                parms[n] = Native.sqlite3_value_int64 (valPtr);
                break;
            case SQLITE_FLOAT:
                parms[n] = Native.sqlite3_value_double (valPtr);
                break;
            case SQLITE3_TEXT:
                parms[n] = Native.sqlite3_value_text16 (valPtr).PtrToString ();
                break;
            case SQLITE_BLOB:
                parms[n] = Native.sqlite3_value_blob (valPtr);
                break;
            case SQLITE_NULL:
                parms[n] = null;
                break;
            default:
                throw new Exception (String.Format ("Value is of unknown type {0}", type));
        }
      }
      return parms;
    }

    /// <summary>
    /// Takes the return value from Invoke() and Final() and figures out how to return it to Sqlite's context.
    /// </summary>
    /// <param name="context">The context the return value applies to</param>
    /// <param name="returnValue">The parameter to return to Sqlite</param>
    void SetReturnValue(IntPtr context, object r)
    {
        if (r is Exception) {
            Native.sqlite3_result_error16 (context, ((Exception)r).Message, -1);
        } else {
            var o = SqliteUtils.ToDbFormat (r);
            if (o == null)
                Native.sqlite3_result_null (context);
            else if (r is int || r is uint)
                Native.sqlite3_result_int (context, (int)r);
            else if (r is long || r is ulong)
                Native.sqlite3_result_int64 (context, (long)r);
            else if (r is double || r is float)
                Native.sqlite3_result_double (context, (double)r);
            else if (r is string) {
                string str = (string)r;
                // -1 for the last arg is the TRANSIENT destructor type so that sqlite will make its own copy of the string
                Native.sqlite3_result_text16 (context, str, str.Length*2, (IntPtr)(-1));
            } else if (r is byte[]) {
                var bytes = (byte[])r;
                Native.sqlite3_result_blob (context, bytes, bytes.Length, (IntPtr)(-1));
            }
        }
    }

    /// <summary>
    /// Internal scalar callback function, which wraps the raw context pointer and calls the virtual Invoke() method.
    /// </summary>
    /// <param name="context">A raw context pointer</param>
    /// <param name="nArgs">Number of arguments passed in</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    internal void ScalarCallback(IntPtr context, int nArgs, IntPtr argsptr)
    {
      SetReturnValue(context, Invoke(ConvertParams(nArgs, argsptr)));
    }

    /// <summary>
    /// Internal collation sequence function, which wraps up the raw string pointers and executes the Compare() virtual function.
    /// </summary>
    /// <param name="len1">Length of the string pv1</param>
    /// <param name="ptr1">Pointer to the first string to compare</param>
    /// <param name="len2">Length of the string pv2</param>
    /// <param name="ptr2">Pointer to the second string to compare</param>
    /// <returns>Returns -1 if the first string is less than the second.  0 if they are equal, or 1 if the first string is greater
    /// than the second.</returns>
    internal int CompareCallback(int len1, IntPtr ptr1, int len2, IntPtr ptr2)
    {
      return Compare(ptr1.PtrToString (), ptr2.PtrToString ());
    }

    /// <summary>
    /// The internal aggregate Step function callback, which wraps the raw context pointer and calls the virtual Step() method.
    /// </summary>
    /// <remarks>
    /// This function takes care of doing the lookups and getting the important information put together to call the Step() function.
    /// That includes pulling out the user's contextData and updating it after the call is made.  We use a sorted list for this so
    /// binary searches can be done to find the data.
    /// </remarks>
    /// <param name="context">A raw context pointer</param>
    /// <param name="nArgs">Number of arguments passed in</param>
    /// <param name="argsptr">A pointer to the array of arguments</param>
    internal void StepCallback(IntPtr context, int nArgs, IntPtr argsptr)
    {
      int n = Native.sqlite3_aggregate_count (context);
      long nAux;
      object obj = null;

      nAux = (long)Native.sqlite3_aggregate_context (context, 1);
      if (n > 1) obj = _contextDataList[nAux];

      Step(ConvertParams(nArgs, argsptr), n, ref obj);
      _contextDataList[nAux] = obj;
    }

    /// <summary>
    /// An internal aggregate Final function callback, which wraps the context pointer and calls the virtual Final() method.
    /// </summary>
    /// <param name="context">A raw context pointer</param>
    internal void FinalCallback(IntPtr context)
    {
      long n = (long)Native.sqlite3_aggregate_context (context, 1);
      object obj = null;

      if (_contextDataList.ContainsKey(n))
      {
        obj = _contextDataList[n];
        _contextDataList.Remove(n);
      }

      SetReturnValue(context, Final(obj));

      IDisposable disp = obj as IDisposable;
      if (disp != null) disp.Dispose();
    }

    /// <summary>
    /// Placeholder for a user-defined disposal routine
    /// </summary>
    /// <param name="disposing">True if the object is being disposed explicitly</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Disposes of any active contextData variables that were not automatically cleaned up.  Sometimes this can happen if
    /// someone closes the connection while a DataReader is open.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);

      IDisposable disp;

      foreach (KeyValuePair<long, object> kv in _contextDataList)
      {
        disp = kv.Value as IDisposable;
        if (disp != null)
          disp.Dispose();
      }
      _contextDataList.Clear();

      _InvokeFunc = null;
      _StepFunc = null;
      _FinalFunc = null;
      _CompareFunc = null;
      _contextDataList = null;

      GC.SuppressFinalize(this);
    }

    const int SQLITE_INTEGER = 1;
    const int SQLITE_FLOAT   = 2;
    const int SQLITE3_TEXT   = 3;
    const int SQLITE_BLOB    = 4;
    const int SQLITE_NULL    = 5;
  }
}
