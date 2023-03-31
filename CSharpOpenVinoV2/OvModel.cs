using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public sealed class OvModel : IDisposable {

    /// <summary>
    /// internal representation of the unmanaged model object.
    /// </summary>
    private readonly IntPtr ov_model_t;

    /// <summary>
    /// constructor based on unmanaged model object.
    /// </summary>
    /// <param name="ov_model_t"></param>
    public OvModel(IntPtr ov_model_t) {
      this.ov_model_t = ov_model_t;
    }

    /// <summary>
    /// retrieve the friendly name of the model.
    /// </summary>
    public string Name {
      get {
        IntPtr name_str = IntPtr.Zero;
        int status = ov_model_get_friendly_name(ov_model_t, ref name_str);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        string name = Marshal.PtrToStringAnsi(name_str);
        return name;
      }
    }

    /// <summary>
    /// destructor of the object.
    /// </summary>
    public void Dispose() {
      ov_model_free(ov_model_t);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_model_free(IntPtr ov_model_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_model_get_friendly_name(IntPtr ov_model_t, ref IntPtr name_str);

    #endregion

  }
}
