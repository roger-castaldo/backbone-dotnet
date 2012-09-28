using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal static class Constants
    {
        public static readonly BindingFlags LOAD_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public static readonly BindingFlags STORE_DATA_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    }
}
