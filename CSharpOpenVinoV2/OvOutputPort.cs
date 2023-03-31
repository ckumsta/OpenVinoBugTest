using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {

  public sealed class OvOutputPort : IDisposable {

    private readonly IntPtr ov_output_port_t;
    private readonly OvElementType type;
    private readonly long Rank;
    private readonly long[] Dims;

    /// <summary>
    /// constructor based on unmanaged output port object.
    /// </summary>
    /// <param name="ov_output_port_t">unmanaged output port object</param>
    public OvOutputPort(IntPtr ov_output_port_t) {
      this.ov_output_port_t = ov_output_port_t;
      int status = 0;
      status = ov_port_get_element_type(this.ov_output_port_t, ref type);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }

      IntPtr shape_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(OvShape)));
      status = ov_const_port_get_shape(this.ov_output_port_t, shape_ptr);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      OvShape shape = Marshal.PtrToStructure<OvShape>(shape_ptr);
      Rank = shape.rank;
      Dims = new long[Rank];
      Marshal.Copy(shape.dims, Dims, 0, (int)Rank);
      Marshal.FreeHGlobal(shape_ptr);
    }

    /// <summary>
    /// friendly string representation of the object.
    /// </summary>
    /// <returns>string representation of the output port</returns>
    public override string ToString() => $"{Name}: [{string.Join(",", Dims)}], {Type}";

    /// <summary>
    /// accessor to the element type of the output port.
    /// </summary>
    public OvElementType Type => type;

    /// <summary>
    /// accessor to the dimensions of the output port.
    /// </summary>
    public long[] Dimensions => Dims;

    /// <summary>
    /// accessor to the name of the output port.
    /// </summary>
    public string Name {
      get {
        IntPtr name_str = IntPtr.Zero;
        int status = ov_port_get_any_name(ov_output_port_t, ref name_str);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        string name = Marshal.PtrToStringAnsi(name_str);
        return name;
      }
    }

    /// <summary>
    /// destructor.
    /// </summary>
    public void Dispose() {
      ov_output_port_free(ov_output_port_t);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_output_port_free(IntPtr outputport);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_port_get_element_type(IntPtr outputport, ref OvElementType type);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_const_port_get_shape(IntPtr outputport, IntPtr shape);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_port_get_any_name(IntPtr outputport, ref IntPtr name_str);
    #endregion
  }
}
