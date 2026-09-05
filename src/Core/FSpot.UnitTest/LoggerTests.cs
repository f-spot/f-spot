using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog;

namespace FSpot.UnitTest
{
	[TestClass]
	public class LoggerTests
	{
		[TestMethod]
		public void BuildLogger_WhenFileSinkCannotOpen_ReturnsUsableLogger ()
		{
			// Null path makes WriteTo.File throw at configuration time, which is
			// the catch this method exists to handle.
			var logger = Logger.BuildLogger (null);

			Assert.AreNotSame (Serilog.Core.Logger.None, logger);
			logger.Information ("file sink failed; console sink must still accept events");
		}
	}
}
