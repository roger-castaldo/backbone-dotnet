using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal static class Properties
    {
        private static bool _compressAllJS=true;
        private static bool _useAppNamespacing=true;

        static Properties()
        {
        }

        public static bool CompressAllJS { get { return _compressAllJS; } }
        public static bool UseAppNamespacing { get { return _useAppNamespacing; } }

        internal static void Init(bool compressAllJS,bool useAppNamespacing)
        {
            _compressAllJS = compressAllJS;
            _useAppNamespacing = useAppNamespacing;
        }
    }
}
