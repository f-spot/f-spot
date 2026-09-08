using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace FSpot.UnitTest
{
	[TestClass]
	public class LoggerTests
	{
		readonly TemporaryDirectories temporaryDirectories = new TemporaryDirectories ();

		[TestCleanup]
		public void RemoveTemporaryDirectories ()
		{
			temporaryDirectories.DeleteAll ();
		}

		// A directory cannot be opened as a file, so WriteTo.File throws at configuration
		// time, which is the catch this method exists to handle. Asserting only that the
		// result is not Logger.None would pass whether or not the fallback ran, so this
		// also asserts the file sink is gone: nothing was written where it was pointed.
		[TestMethod]
		public void BuildLogger_WhenFileSinkCannotOpen_FallsBackToAConsoleOnlyLogger ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (directory);

			using (logger as IDisposable) {
				Assert.AreNotSame (Serilog.Core.Logger.None, logger);
				logger.Information ("file sink failed; console sink must still accept events");
			}

			Assert.IsEmpty (Directory.GetFiles (directory), "the fallback logger must have no file sink");
		}

		// The Serilog 2.10 -> 4.4 bump crosses two majors, and 94 call sites go through
		// this configuration. These exercise it end to end rather than only checking that
		// BuildLogger returns something.
		[TestMethod]
		public void BuildLogger_WritesTheMessageToTheFileSink ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (Path.Combine (directory, "f-spot.txt"));

			using (logger as IDisposable)
				logger.Information ("a message that has to reach the file, {Value}", 42);

			Assert.Contains ("a message that has to reach the file, 42", ReadTheLog (directory));
		}

		[TestMethod]
		public void BuildLogger_HonoursTheLevelSwitchForEachLevel ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (Path.Combine (directory, "f-spot.txt"));

			try {
				using (logger as IDisposable) {
					Logger.SetLevel (LogEventLevel.Warning);
					logger.Debug ("debug is below the switch");
					logger.Information ("information is below the switch");
					logger.Warning ("warning is at the switch");
					logger.Error ("error is above the switch");
				}

				string log = ReadTheLog (directory);
				Assert.DoesNotContain ("below the switch", log);
				Assert.Contains ("warning is at the switch", log);
				Assert.Contains ("error is above the switch", log);
			} finally {
				Logger.SetLevel (LogEventLevel.Information);
			}
		}

		// Deliberately not a test of Enrich.WithExceptionDetails: removing that enricher
		// does not fail this, because Serilog's default output template renders
		// {Exception} on its own, so the enricher's structured properties are invisible
		// through a plain text sink. What this does pin is that an exception passed
		// alongside a message reaches the log with both intact.
		[TestMethod]
		public void BuildLogger_WritesTheExceptionAlongsideTheMessage ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (Path.Combine (directory, "f-spot.txt"));

			using (logger as IDisposable)
				logger.Error (new InvalidOperationException ("the inner detail"), "something failed");

			string log = ReadTheLog (directory);
			Assert.Contains ("something failed", log);
			Assert.Contains ("the inner detail", log);
		}

		// What BuildLogger's text sink structurally cannot show: Serilog.Exceptions
		// (8.1 -> 8.4 here) attaches its data as a structured "ExceptionDetail" property,
		// and the default template has no {Properties} token to render it. Asserted
		// against a capturing sink instead, so the bump is covered rather than assumed.
		[TestMethod]
		public void WithExceptionDetails_AttachesTheExceptionDetailProperty ()
		{
			var captured = new List<LogEvent> ();

			using (var logger = new LoggerConfiguration ()
					.Enrich.WithExceptionDetails ()
					.WriteTo.Sink (new CapturingSink (captured))
					.CreateLogger ())
				logger.Error (new InvalidOperationException ("the inner detail"), "something failed");

			var single = Assert.ContainsSingle (captured);
			Assert.Contains ("ExceptionDetail", single.Properties.Keys);
			Assert.Contains ("the inner detail", single.Properties["ExceptionDetail"].ToString ());
		}

		sealed class CapturingSink : ILogEventSink
		{
			readonly List<LogEvent> events;

			public CapturingSink (List<LogEvent> events) => this.events = events;

			public void Emit (LogEvent logEvent) => events.Add (logEvent);
		}

		static string ReadTheLog (string directory)
		{
			string[] files = Directory.GetFiles (directory);
			Assert.IsNotEmpty (files, "the file sink should have created a log file");

			return string.Join (Environment.NewLine, files.Select (File.ReadAllText));
		}
	}
}
