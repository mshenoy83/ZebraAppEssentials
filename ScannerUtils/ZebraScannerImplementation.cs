using Android.App;
using Android.Content;
using AppEssentials.ActivityUtils;
using AppEssentials.EventHandling;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using System;
using System.Collections.Generic;

namespace AppEssentials.ScannerUtils
{
    public class ZebraScannerImplementation : Java.Lang.Object, IZebraScanner, EMDKManager.IEMDKListener
    {

        public event EventHandler<BarCodeAccessArgs> BarcodeRequested
        {
            add
            {
                CrossWeakEventManager.Current.AddEventHandler("BarcodeRequested", value);
            }

            remove
            {
                CrossWeakEventManager.Current.RemoveEventHandler("BarcodeRequested", value);
            }
        }

        // Declare a variable to store EMDKManager object
        private EMDKManager emdkManager = null;

        // Declare a variable to store BarcodeManager object
        private BarcodeManager barcodeManager = null;

        // Declare a variable to store Scanner object
        private Scanner scanner = null;

        // Declare a flag for continuous scan mode
        private bool isContinuousMode = true;

        // Declare a flag to save the current state of continuous mode flag during OnPause() and Bluetooth scanner Disconnected event.
        private bool isContinuousModeSaved = true;

        private string statusString = "";

        private IList<ScannerInfo> scannerList = null;

        private int scannerIndex = 0; // Keep the selected scanner
        private int defaultIndex = 0; // Keep the default scanner 
        private int triggerIndex = 0; // Keep the selected trigger

        private int ScanningStartCount = 0;

        ActivityLifecycleContextListener lifecycleListener;

        /// <summary>
        /// Gets the current application context
        /// </summary>
        public Context AppContext => Application.Context;

        public void Init(Application application)
        {
            if (lifecycleListener != null)
                return;

            lifecycleListener = new ActivityLifecycleContextListener();
            application.RegisterActivityLifecycleCallbacks(lifecycleListener);
        }

        public void BootScanner()
        {
            EMDKResults results = EMDKManager.GetEMDKManager(AppContext, this);
            //if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            //{
            //    statusView.Text = "Status: EMDKManager object creation failed ...";
            //}
            //else
            //{
            //    statusView.Text = "Status: EMDKManager object creation succeeded ...";
            //}
        }

        public void RestartScanner()
        {
            // Acquire the barcode manager resources
            if (emdkManager != null)
            {
                try
                {
                    barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

                    if (barcodeManager != null)
                    {
                        // Add connection listener
                        barcodeManager.Connection += BarcodeManager_Connection;
                    }

                    // Enumerate scanners 
                    EnumerateScanners();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.StackTrace);
                }
            }
        }

        public void DisposeScanner(bool disposeEMDKManager = false)
        {
            // De-initialize scanner
            TerminateScanner();

            // Clean up the objects created by EMDK manager
            if (barcodeManager != null)
            {
                // Remove connection listener
                barcodeManager.Connection -= BarcodeManager_Connection;
                barcodeManager = null;
                // Clear scanner list
                scannerList = null;
            }

            // Release EMDK Object
            if (emdkManager != null)
            {
                emdkManager.Release(EMDKManager.FEATURE_TYPE.Barcode);
                if (disposeEMDKManager)
                {
                    emdkManager = null;
                }
            }
        }

        public void OnClosed()
        {
            try
            {
                // This callback will be issued when the EMDK closes unexpectedly.
                if (emdkManager != null)
                {

                    if (barcodeManager != null)
                    {
                        // Remove connection listener
                        barcodeManager.Connection -= BarcodeManager_Connection;
                        barcodeManager = null;
                    }

                    // Release all the resources
                    emdkManager.Release();
                    emdkManager = null;
                }
            }
            catch (Exception ex)
            {
                CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : EMDK closed unexpectedly! Please close and restart the application.", false), "BarcodeRequested");
            }
        }

        public void OnOpened(EMDKManager emdkManagerInstance)
        {
            emdkManager = emdkManagerInstance;

            try
            {

                // Acquire the barcode manager resources
                barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

                if (barcodeManager != null)
                {
                    // Add connection listener
                    barcodeManager.Connection += BarcodeManager_Connection;
                }

                // Enumerate scanner devices
                EnumerateScanners();

                StartScan();
            }
            catch (Exception ex)
            {
                CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : BarcodeManager object creation failed." + ex.Message, false), "BarcodeRequested");
            }
        }

        private void EnumerateScanners()
        {
            if (barcodeManager != null)
            {
                int spinnerIndex = 0;
                List<string> friendlyNameList = new List<string>();

                // Query the supported scanners on the device
                scannerList = barcodeManager.SupportedDevicesInfo;

                if ((scannerList != null) && (scannerList.Count > 0))
                {
                    foreach (ScannerInfo scnInfo in scannerList)
                    {
                        friendlyNameList.Add(scnInfo.FriendlyName);

                        // Save index of the default scanner (device specific one)
                        if (scnInfo.IsDefaultScanner)
                        {
                            defaultIndex = spinnerIndex;
                        }

                        ++spinnerIndex;
                    }
                }
                else
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : Failed to get the list of supported scanner devices! Please close and restart the application.", false), "BarcodeRequested");
                }
            }
        }

        void BarcodeManager_Connection(object sender, BarcodeManager.ScannerConnectionEventArgs e)
        {
            string status = string.Empty;
            string scannerName = string.Empty;

            ScannerInfo scannerInfo = e.P0;
            BarcodeManager.ConnectionState connectionState = e.P1;

            string statusBT = connectionState.ToString();
            string scannerNameBT = scannerInfo.FriendlyName;

            if (scannerList.Count != 0)
            {
                scannerName = scannerList[scannerIndex].FriendlyName;
            }

            if (scannerName.ToLower().Equals(scannerNameBT.ToLower()))
            {
                if (connectionState == BarcodeManager.ConnectionState.Connected)
                {
                    // Bluetooth scanner connected
                    // Restore continuous mode flag
                    isContinuousMode = isContinuousModeSaved;

                    // Initialize scanner
                    InitScanner();
                    SetTrigger();
                    SetDecoders();
                }

                if (connectionState == BarcodeManager.ConnectionState.Disconnected)
                {
                    // Bluetooth scanner disconnected

                    // Save the current state of continuous mode flag
                    isContinuousModeSaved = isContinuousMode;

                    // Reset continuous flag 
                    isContinuousMode = false;

                    // De-initialize scanner
                    TerminateScanner();
                }
            }
            else
            {
                status = "Status: " + statusString + " " + scannerNameBT + ":" + statusBT;
                CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Status : " + status, false), "BarcodeRequested");
            }
        }

        /// <summary>
        /// Fuction to set the trigger type, User need to trigger the button to scan the item or will be the automatic trigger
        /// </summary>
        private void SetTrigger()
        {
            if (scanner == null)
            {
                InitScanner();
            }

            if (scanner != null)
            {
                switch (triggerIndex)
                {
                    case 0: // Selected "HARD"
                        scanner.TriggerType = Scanner.TriggerTypes.Hard;
                        break;
                    case 1: // Selected "SOFT"
                        scanner.TriggerType = Scanner.TriggerTypes.SoftAlways;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Function Used to Start the Scanning 
        /// </summary>
        private void StartScan()
        {
            if (scanner == null)
            {
                InitScanner();

                SetScannerConfig();
            }

            if (scanner != null)
            {
                try
                {
                    // Set continuous flag
                    isContinuousMode = true;

                    if (scanner.IsReadPending == false)
                    {
                        // Submit a new read.
                        scanner.Read();
                    }
                }
                catch (ScannerException ex)
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : Unable to Start Scan : " + ex.Message, false), "BarcodeRequested");
                }
            }
        }

        /// <summary>
        /// Function Used to Initiate the Scanner
        /// </summary>
        private void InitScanner()
        {
            if (scanner == null)
            {
                if ((scannerList != null) && (scannerList.Count > 0))
                {
                    // Get new scanner device based on the selected index
                    scanner = barcodeManager.GetDevice(scannerList[defaultIndex]);
                }
                else
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : Failed to get the specified scanner device! Please close and restart the application.", false), "BarcodeRequested");
                    return;
                }

                if (scanner != null)
                {
                    // Add data listener
                    scanner.Data += Scanner_Data;

                    // Add status listener
                    scanner.Status += Scanner_Status;

                    try
                    {
                        // Enable the scanner
                        scanner.Enable();
                    }
                    catch (ScannerException ex)
                    {
                        CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : Unable to Start Scan : " + ex.Message, false), "BarcodeRequested");
                    }
                }
                else
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : Failed to initialize the scanner device", false), "BarcodeRequested");
                }
            }
        }


        private void SetDecoders()
        {
            if (scanner == null)
            {
                InitScanner();
            }

            SetScannerConfig();
        }

        /// <summary>
        /// Function Used to disable the scanner and remove listener
        /// </summary>
        private void TerminateScanner()
        {
            if (scanner != null)
            {
                try
                {
                    // Cancel if there is any pending read
                    scanner.CancelRead();

                    // Disable the scanner 
                    scanner.Disable();
                }
                catch (ScannerException e)
                {
                    Console.WriteLine("Status: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                // Remove data listener
                scanner.Data -= Scanner_Data;

                // Remove status listener
                scanner.Status -= Scanner_Status;

                try
                {
                    // Release the scanner
                    scanner.Release();
                }
                catch (ScannerException ex)
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Error : While Release the Scanner : " + ex.Message, false), "BarcodeRequested");
                }

                scanner = null;
            }
        }

        /// <summary>
        /// Scanners read the barcode and provide the data using that function
        /// </summary>
        /// <param name="sender">Scanner</param>
        /// <param name="e">Scanner Data Arguments</param>
        void Scanner_Data(object sender, Scanner.DataEventArgs e)
        {
            ScanDataCollection scanDataCollection = e.P0;

            if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
            {
                IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();

                foreach (ScanDataCollection.ScanData data in scanData)
                {
                    if (data != null)
                    {
                        if (!string.IsNullOrEmpty(data.Data))
                        {
                            string code = data.Data.ToString();
                            CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs(code, true), "BarcodeRequested");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Function will return the current status of the Scanner, For Example :-- Idle, Waiting For Trigger
        /// </summary>
        /// <param name="sender">Scanner</param>
        /// <param name="e">Scanner Status Event Arguments</param>
        void Scanner_Status(object sender, Scanner.StatusEventArgs e)
        {
            StatusData statusData = e.P0;
            StatusData.ScannerStates state = e.P0.State;

            if (state == StatusData.ScannerStates.Idle)
            {
                if (statusData != null)
                {
                    if (statusData.FriendlyName != null)
                    {
                        statusString = "Status: " + statusData.FriendlyName + " is enabled and idle...";
                    }
                }


                if (isContinuousMode)
                {
                    try
                    {
                        // An attempt to use the scanner continuously and rapidly (with a delay < 100 ms between scans) 
                        // may cause the scanner to pause momentarily before resuming the scanning. 
                        // Hence add some delay (>= 100ms) before submitting the next read.
                        try
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }

                        if (scanner != null)
                        {
                            if (scanner.IsReadPending == false)
                            {
                                // Submit another read to keep the continuation
                                scanner.Read();
                            }
                        }
                    }
                    catch (ScannerException ex)
                    {
                        statusString = "Error : " + ex.Message;

                        Console.WriteLine(ex.StackTrace);
                    }
                    catch (NullReferenceException ex)
                    {
                        statusString = "Error : An error has occurred.";
                        CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs(statusString, false), "BarcodeRequested");
                        Console.WriteLine(ex.StackTrace);
                    }
                    catch (Exception ex) //Added 2nd March 2017
                    {
                        statusString = "Exception in Scanner_Status event handler";
                        CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs(statusString, false), "BarcodeRequested");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }

            if (state == StatusData.ScannerStates.Waiting)
            {
                statusString = "Status: Scanner is waiting for trigger press...";

            }

            if (state == StatusData.ScannerStates.Scanning)
            {
                statusString = "Status: Scanning...";
            }

            if (state == StatusData.ScannerStates.Disabled)
            {
                statusString = "Status: " + statusData.FriendlyName + " is disabled.";
            }

            if (state == StatusData.ScannerStates.Error)
            {
                statusString = "Status: An error has occurred.";
            }
        }

        #region Barcode Configuration

        /// <summary>
        /// We have different type of barcode types
        /// This is the main function to set the different barcode types. 
        /// </summary>
        private void SetScannerConfig()
        {
            if (scanner != null)
            {
                try
                {
                    // Config object should be taken out before changing.
                    ScannerConfig config = scanner.GetConfig();

                    // Set EAN8
                    config.DecoderParams.Ean8.Enabled = true;

                    // Set EAN13
                    config.DecoderParams.Ean13.Enabled = true;

                    // Set Code39
                    config.DecoderParams.Code39.Enabled = true;

                    // Set Code128
                    config.DecoderParams.Code128.Enabled = true;

                    // Set Invertible 2 of 5
                    config.DecoderParams.I2of5.Enabled = true;

                    // Enable the picklist mode
                    config.ReaderParams.ReaderSpecific.ImagerSpecific.PicklistEx = ScannerConfig.PicklistEx.Hardware;

                    // Should be assigned back to the property to get the changes to the lower layers.
                    scanner.SetConfig(config);
                }
                catch (ScannerException ex)
                {
                    CrossWeakEventManager.Current.HandleEvent(null, new BarCodeAccessArgs("Info : Unable to Set Scanner Config : " + ex.Message, false), "BarcodeRequested");
                }
            }
        }

        #endregion
    }
}
