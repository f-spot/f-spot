//
// Mono.Google.GoogleService.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix  (stephane@delcroix.org)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 S. Delcroix
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
namespace Mono.Google {
	public class GoogleService {
		public static GoogleService Picasa {
			get { return new GoogleService ("lh2"); }
		}

		public static GoogleService Calendar {
			get { return new GoogleService ("cl"); }
		}

		string service_code;
		public string ServiceCode {
			get { return service_code; }
		}

		private GoogleService (string service_code)
		{
			this.service_code = service_code;
		}
	}
}
