using System;
using System.Collections.Generic;
using System.Text;

namespace AppEssentials.ScannerUtils
{
    public class BarCodeAccessArgs: EventArgs
	{
		public string BarCode { get;}

		public bool IsBarCodeReceived { get;}

		public BarCodeAccessArgs(string barcode, bool barcodereceived)
		{
			BarCode = barcode;
			IsBarCodeReceived = barcodereceived; 
		}
	}
}
