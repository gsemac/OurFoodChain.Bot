using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ArrayUtils {

        // https://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp/248613#248613

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        public sealed class NaturalStringComparer : IComparer<string> {
            public int Compare(string a, string b) {
                return SafeNativeMethods.StrCmpLogicalW(a, b);
            }
        }

    }

}
