using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AppEssentials.ScannerUtils;
using System;


namespace AppEssentials.ActivityUtils
{
    [Preserve(AllMembers = true)]
    internal class ActivityLifecycleContextListener : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        WeakReference<Activity> currentActivity = new WeakReference<Activity>(null);

        public Context Context =>
            Activity ?? Application.Context;

        public Activity Activity
        {
            get => currentActivity.TryGetTarget(out var a) ? a : null;
            set => currentActivity.SetTarget(value);
        }

        ZebraScannerImplementation Current =>
            (ZebraScannerImplementation)(CrossZebraScanner.Current);

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            Activity = activity;
            if (Activity is IActivityScannerSupport activityScannerSupport)
            {
                Current.BootScanner();                
            }
        }

        public void OnActivityDestroyed(Activity activity)
        {
            if (activity is IActivityScannerSupport activityScannerSupport)
            {
                Current.DisposeScanner(true);
            }
        }

        public void OnActivityPaused(Activity activity)
        {
            Activity = activity;
            if (activity is IActivityScannerSupport activityScannerSupport)
            {
                Current.DisposeScanner();
            }

        }

        public void OnActivityResumed(Activity activity)
        {
            Activity = activity;
            if (activity is IActivityScannerSupport activityScannerSupport)
            {
                Current.RestartScanner();
            }
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {

        }

        public void OnActivityStarted(Activity activity)
        {

        }

        public void OnActivityStopped(Activity activity)
        {

        }
    }
}
