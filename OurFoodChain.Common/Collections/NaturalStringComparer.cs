using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace OurFoodChain.Common.Collections {

    // https://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp/248613#248613

    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods {

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);

    }

    public sealed class NaturalStringComparer :
        IComparer<string> {

        public int Compare(string a, string b) {

            return SafeNativeMethods.StrCmpLogicalW(a, b);

        }

    }

}