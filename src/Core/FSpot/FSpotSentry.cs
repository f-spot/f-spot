//
// FSpotSentry.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using FSpot.Settings;

using Sentry;
using Sentry.Protocol;

namespace FSpot
{
	// Shared Sentry options for every desktop client. Reporting follows the
	// preference, which defaults on. FSPOT_SENTRY=0 forces it off. Tests call
	// Configure and never Init, so CI cannot ship events to the project DSN.
	public static class FSpotSentry
	{
		public const string GtkClient = "gtk";
		public const string AvaloniaClient = "avalonia";
		public const string KillSwitchVariable = "FSPOT_SENTRY";

		public const string EnvironmentName =
#if DEBUG
			"development";
#else
			"production";
#endif

		const string ClientTag = "client";
		const string Dsn =
			"https://6cdc2b33312541c5ac9c3a61e1c960d0@o4509369388761088.ingest.us.sentry.io/4509568228851712";

		static readonly Regex AbsolutePath = new Regex (
			@"(?<![A-Za-z0-9_])(?:[A-Za-z]:[\\/][^\s""'<>]+|/(?:[\w.+-]+/)+[\w.+-]+)",
			RegexOptions.Compiled);
		static readonly Regex TildePath = new Regex (@"~/[^\s""'<>]+", RegexOptions.Compiled);

		public static Func<IEnumerable<string>> TagNames = () => Array.Empty<string> ();

		public static bool IsKillSwitchOff (string value)
		{
			if (string.IsNullOrWhiteSpace (value))
				return false;
			return value == "0"
				|| value.Equals ("false", StringComparison.OrdinalIgnoreCase)
				|| value.Equals ("off", StringComparison.OrdinalIgnoreCase)
				|| value.Equals ("no", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsRequested ()
		{
			if (IsKillSwitchOff (Environment.GetEnvironmentVariable (KillSwitchVariable)))
				return false;
			try {
				return LibraryPreferences.SendCrashReports;
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				return false;
			}
		}

		public static IDisposable TryInit (string client)
		{
			if (!IsRequested ())
				return Disabled.Instance;
			return SentrySdk.Init (o => Configure (o, client));
		}

		public static void Apply (string client)
		{
			if (IsRequested ()) {
				if (!SentrySdk.IsEnabled)
					SentrySdk.Init (o => Configure (o, client));
				return;
			}

			if (SentrySdk.IsEnabled)
				SentrySdk.Close ();
		}

		public static void Configure (SentryOptions options, string client)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));
			if (string.IsNullOrEmpty (client))
				throw new ArgumentException ("client is required", nameof (client));

			options.Dsn = Dsn;
			options.Release = $"f-spot@{FSpotConfiguration.Version}";
			options.Environment = EnvironmentName;
			options.IsGlobalModeEnabled = true;
			options.SendDefaultPii = false;
			options.DefaultTags[ClientTag] = client;
			options.SetBeforeSend ((evt, _) => ScrubEvent (evt));
			options.SetBeforeBreadcrumb ((crumb, _) => ScrubBreadcrumb (crumb));
		}

		public static string Scrub (string text)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			string home = FSpotConfiguration.HomeDirectory;
			if (!string.IsNullOrEmpty (home)) {
				text = ReplacePath (text, home);
				string slashed = home.Replace ('\\', '/');
				if (!string.Equals (slashed, home, StringComparison.Ordinal))
					text = ReplacePath (text, slashed);
				string backed = home.Replace ('/', '\\');
				if (!string.Equals (backed, home, StringComparison.Ordinal))
					text = ReplacePath (text, backed);
			}

			IEnumerable<string> tags;
			try {
				tags = TagNames?.Invoke () ?? Array.Empty<string> ();
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				tags = Array.Empty<string> ();
			}

			foreach (string tag in tags.Where (name => !string.IsNullOrWhiteSpace (name)).OrderByDescending (name => name.Length))
				text = ReplaceOrdinal (text, tag, "[tag]");

			text = TildePath.Replace (text, "~");
			return AbsolutePath.Replace (text, "[path]");
		}

		public static SentryEvent ScrubEvent (SentryEvent evt)
		{
			if (evt == null)
				return null;
			if (evt.Message != null) {
				evt.Message = new SentryMessage {
					Message = Scrub (evt.Message.Message),
					Formatted = Scrub (evt.Message.Formatted)
				};
			}

			if (evt.SentryExceptions != null) {
				foreach (SentryException exception in evt.SentryExceptions)
					exception.Value = Scrub (exception.Value);
			}

			return evt;
		}

		public static Breadcrumb ScrubBreadcrumb (Breadcrumb crumb)
		{
			if (crumb == null)
				return null;
			return new Breadcrumb (Scrub (crumb.Message), crumb.Type, crumb.Data, crumb.Category, crumb.Level);
		}

		static string ReplacePath (string text, string path)
			=> ReplaceOrdinal (text, path, "~");

		static string ReplaceOrdinal (string text, string oldValue, string newValue)
		{
			if (string.IsNullOrEmpty (text) || string.IsNullOrEmpty (oldValue))
				return text;
			int index = text.IndexOf (oldValue, StringComparison.Ordinal);
			if (index < 0)
				return text;
			var builder = new System.Text.StringBuilder ();
			int start = 0;
			while (index >= 0) {
				builder.Append (text, start, index - start);
				builder.Append (newValue);
				start = index + oldValue.Length;
				index = text.IndexOf (oldValue, start, StringComparison.Ordinal);
			}
			builder.Append (text, start, text.Length - start);
			return builder.ToString ();
		}

		sealed class Disabled : IDisposable
		{
			public static readonly Disabled Instance = new Disabled ();
			public void Dispose () { }
		}
	}
}
