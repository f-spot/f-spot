namespace FSpot.Iptc {
	public class DataSet {
		public byte RecordNumber;
		public byte DataSetNumber;
		public byte [] Data;
		
		const byte TagMarker = 0x1c;
		const uint LengthMask = 1 << 15;

		public void Load (System.IO.Stream stream)
		{
			byte [] rec = new byte [5];
			stream.Read (rec, 0, rec.Length);
			if (rec [0] != TagMarker)
				throw new System.Exception (System.String.Format ("Invalid tag marker found {0} != 0x1c", 
							    TagMarker.ToString ("x")));
			
			RecordNumber = rec [1];
			DataSetNumber = rec [2];

			ulong length = FSpot.BitConverter.ToUInt16 (rec, 3, false);			

			if ((length & (LengthMask)) > 0)
				throw new System.Exception ("long records not currently supported");

			// FIXME if the length is greater than 32768 we re
			Data = new byte [length];
			stream.Read (Data, 0, Data.Length);
		}

		public void Save (System.IO.Stream stream)
		{
			stream.WriteByte (TagMarker);
			stream.WriteByte (RecordNumber);
			stream.WriteByte (DataSetNumber);
		}
	}

	public class IptcFile {
		System.Collections.ArrayList sets = new System.Collections.ArrayList ();
		
		public IptcFile (System.IO.Stream stream)
		{
			Load (stream);
		}
		
		public void Load (System.IO.Stream stream)
		{
			while (stream.Position < stream.Length) {
				DataSet dset = new DataSet ();
				dset.Load (stream);
				System.Console.WriteLine ("{0}:{1}", dset.RecordNumber, dset.DataSetNumber);
				sets.Add (dset);
			}
		}

		public void Save (System.IO.Stream stream) 
		{
			foreach (DataSet dset in sets) {
				dset.Save (stream);
			}
		}
	}
}
