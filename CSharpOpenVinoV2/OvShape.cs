using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {

  /// <summary>
  /// structure used for unmanaged shape retrieval.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct OvShape {
    public long rank;
    public IntPtr dims;
  }
}
