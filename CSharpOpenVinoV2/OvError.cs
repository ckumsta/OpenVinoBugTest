using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public static class OvError {

    /// <summary>
    /// get error message string from error code.
    /// </summary>
    /// <param name="code">error code</param>
    /// <returns>error message</returns>
    public static string GetErrorFromCode(int code) {
      IntPtr str_ptr = ov_get_error_info(code);        // get the char* from ov
      var error_msg = Marshal.PtrToStringAnsi(str_ptr); // convert char* into string
      return error_msg;
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ov_get_error_info(int ov_status);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_free(IntPtr error_info); // supposed to do that, but we get an exception if we try to free the char*
    #endregion
  }
}
