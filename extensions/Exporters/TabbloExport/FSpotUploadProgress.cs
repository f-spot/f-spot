//
// FSpotTabbloExport.FSpotUploadProgress
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2008 Wojciech Dzierzanowski
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

using Mono.Tabblo;
using Mono.Unix;
using System;

namespace FSpotTabbloExport {

	class FSpotUploadProgress : TotalUploadProgress	{

		private FSpot.ThreadProgressDialog progress_dialog;


		internal FSpotUploadProgress (
				Picture [] pictures,
				FSpot.ThreadProgressDialog progress_dialog)
			: base (pictures)
		{
			this.progress_dialog = progress_dialog;
		}


		protected override void ShowProgress (string title,
		                                      long bytes_sent)
		{
			progress_dialog.Message = title;
			progress_dialog.ProgressText = String.Format (
					Catalog.GetString (
							"{0} of approx. {1}"),
					SizeUtil.ToHumanReadable (bytes_sent),
					SizeUtil.ToHumanReadable (
							(long) TotalFileSize));
			progress_dialog.Fraction =
					(double) bytes_sent / TotalFileSize;
		}
	}
}
