using System;

namespace FSpot.Database
{
    public class DbException : ApplicationException
    {
        public DbException (string msg) : base(msg)
        {
        }
    }
}
