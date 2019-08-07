using Android.App;
using System;

namespace AppEssentials.ScannerUtils
{
    public interface IZebraScanner
    {
        void Init(Application application);
        event EventHandler<BarCodeAccessArgs> BarcodeRequested;
        void BootScanner();
        void RestartScanner();
        void DisposeScanner(bool disposeEMDKManager = false);
    }
}
