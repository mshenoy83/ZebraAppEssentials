using System;

namespace AppEssentials.ScannerUtils
{
    public class CrossZebraScanner
    {
        static Lazy<IZebraScanner> implementation = new Lazy<IZebraScanner>(() => CreateZebraScanner(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current settings to use
        /// </summary>
        public static IZebraScanner Current
        {
            get
            {
                var ret = implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IZebraScanner CreateZebraScanner()
        {
#if MONOANDROID81 || MONOANDROID44
            return new ZebraScannerImplementation();
#else
            return null;
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}
