//
// FaceDetectionJob.cs
//
// Author:
//   Valentín Barros <valentin@sanva.net>
//
// Copyright (C) 2013 Valentín Barros
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using FSpot.Core;
using FSpot.Widgets;

using Banshee.Kernel;

using Hyena;

namespace FSpot.Jobs {
	public class FaceDetectionJob : IJob {
		private Queue<string> faces = null;
		private string image_path;

		public FaceDetectionJob (string image_path)
		{
			this.image_path = image_path;
		}
		
		public void Run ()
		{
			string output;
			try {
				Process process = new Process ();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.FileName = "f-spot-facedetect";
				process.StartInfo.Arguments = "--cascade=\"" +
					Path.Combine (Global.FacedetectDirectory, "facedetect-haarcascade.xml") +
					"\" --scale=1.2 \"" + image_path + "\"";
				process.Start ();
				output = process.StandardOutput.ReadToEnd ();
				process.WaitForExit ();
			} catch (Exception e) {
				Log.DebugFormat ("Error trying to spawn face detection program: {0}", e.Message);
				return;
			}

			faces = new Queue<string> ();
			string [] lines = output.Split (new char [] {'\n'});
			foreach (string line in lines) {
				if (line.Length == 0)
					continue;
				
				string [] type_and_serialized = line.Split (new char [] {';'});
				if (type_and_serialized.Length != 2)
					throw new Exception ("Wrong serialized line in face detection program output.");
				
				switch (type_and_serialized [0]) {
				case "face":
					StringBuilder serialized_geometry = new StringBuilder (3);
					serialized_geometry.Append (FaceRectangle.SHAPE_TYPE);
					serialized_geometry.Append (";");
					serialized_geometry.Append (ParseSerializedGeometry (type_and_serialized [1]));
					
					faces.Enqueue (serialized_geometry.ToString ());
					break;
				case "warning":
					Log.Debug (type_and_serialized [1]);
					break;
				case "error":
					throw new Exception (type_and_serialized [1]);
				default:
					throw new Exception ("Can't understand face detection program output.");
				}
			}
		}
		
		private string ParseSerializedGeometry (string serialized_geometry)
		{
			string [] serialized_geometry_pieces = serialized_geometry.Split (new char [] {'&'});
			if (serialized_geometry_pieces.Length != 4)
				throw new Exception ("Wrong serialized line in face detection program output.");
			
			double x = 0;
			double y = 0;
			double width = 0;
			double height = 0;
			foreach (string piece in serialized_geometry_pieces) {
				string [] name_and_value = piece.Split(new char [] {'='});
				if (name_and_value.Length != 2)
					throw new Exception ("Wrong serialized line in face detection program output.");

				double value = Double.Parse (name_and_value [1], CultureInfo.InvariantCulture);
				switch (name_and_value [0]) {
				case "x":
					x = value;
					break;
				case "y":
					y = value;
					break;
				case "width":
					width = value;
					break;
				case "height":
					height = value;
					break;
				default:
					throw new Exception ("Wrong serialized line in face detection program output.");
				}
			}
			
			double half_width = width / 2.0;
			double half_height = height / 2.0;
			return String.Format ("{0};{1};{2};{3}",
			                      (x + half_width).ToString ("R", CultureInfo.InvariantCulture),
			                      (y + half_height).ToString ("R", CultureInfo.InvariantCulture),
			                      half_width.ToString ("R", CultureInfo.InvariantCulture),
			                      half_height.ToString ("R", CultureInfo.InvariantCulture));
		}
		
		public string GetNext ()
		{
			if (faces == null || faces.Count == 0)
				return null;
			
			return faces.Dequeue ();
		}
	}
}
