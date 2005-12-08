using System;
using System.Runtime.InteropService;

namespace FSpot.Scanning {
	public enum Status {
		Good,
		Unsupported,
		Canceled,
		DeviceBusy,
		Invalid,
		Eof,
		Jammed,
		NoDocuments,
		CoverOpen,
		InputOuputError,
		OutOfMemory,
		AccessDenied
	}
}
