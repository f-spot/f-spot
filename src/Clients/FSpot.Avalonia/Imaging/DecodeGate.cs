// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading;

namespace FSpot.Avalonia.Imaging;

// One under-cap photo was measured near 800 MB, so a second decode waits.
static class DecodeGate
{
	internal const int MaxConcurrent = 1;

	static readonly SemaphoreSlim gate = new (MaxConcurrent, MaxConcurrent);
	static readonly ThreadLocal<int> depth = new ();

	internal static int LastRunThreadId { get; private set; }

	internal static void ForgetLastRunThread ()
		=> LastRunThreadId = 0;

	internal static IDisposable Enter ()
	{
		gate.Wait ();
		return new Held ();
	}

	internal static T Run<T> (Func<T> decode)
	{
		if (depth.Value > 0)
			return decode ();

		gate.Wait ();
		depth.Value = 1;
		try {
			LastRunThreadId = Environment.CurrentManagedThreadId;
			return decode ();
		} finally {
			depth.Value = 0;
			gate.Release ();
		}
	}

	internal static async Task<T> RunAsync<T> (Func<T> decode, CancellationToken cancellationToken)
	{
		if (depth.Value > 0)
			return decode ();

		await gate.WaitAsync (cancellationToken).ConfigureAwait (false);
		try {
			return await Task.Run (() => {
				depth.Value = 1;
				try {
					LastRunThreadId = Environment.CurrentManagedThreadId;
					return decode ();
				} finally {
					depth.Value = 0;
				}
			}, cancellationToken).ConfigureAwait (false);
		} finally {
			gate.Release ();
		}
	}

	sealed class Held : IDisposable
	{
		bool disposed;

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			gate.Release ();
		}
	}
}
