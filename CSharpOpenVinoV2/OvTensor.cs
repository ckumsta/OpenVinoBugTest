using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public sealed class OvTensor : IDisposable {

    private readonly IntPtr ov_tensor_t;

    /// <summary>
    /// constructor based on unmanaged ov_tensor_t object.
    /// </summary>
    /// <param name="ov_tensor_t">internal unmanaged object</param>
    public OvTensor(IntPtr ov_tensor_t) {
      this.ov_tensor_t = ov_tensor_t;
    }

    /// <summary>
    /// constructor based on tensor properties (allocate unmanaged memory).
    /// </summary>
    /// <param name="dims">dimensions of the tensor</param>
    /// <param name="type">element type of the tensor</param>
    public OvTensor(long[] dims, OvElementType type) {
      // start creating the dims unmanaged data
      IntPtr dims_ptr = Marshal.AllocHGlobal(dims.Length * sizeof(long));
      Marshal.Copy(dims, 0, dims_ptr, dims.Length);

      // then prepare the shape struct
      var shape = new OvShape { rank = dims.Length, dims = dims_ptr };

      // create the ov_shape_t unmanaged
      IntPtr ov_shape_t = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(OvShape)));
      Marshal.StructureToPtr<OvShape>(shape, ov_shape_t, false);

      // create the tensor object
      int status = ov_tensor_create((int)type, ov_shape_t, ref ov_tensor_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }

      // free all unmanaged data
      Marshal.FreeHGlobal(ov_shape_t);
      Marshal.FreeHGlobal(dims_ptr);
    }

    /// <summary>
    /// retrieve the size of the tensor in elements unit.
    /// </summary>
    public int SizeElements {
      get {
        int size = 0;
        int status = ov_tensor_get_size(ov_tensor_t, ref size);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return size;
      }
    }

    /// <summary>
    /// retrieve the size of the tensor in bytes unit.
    /// </summary>
    public int SizeBytes {
      get {
        int size = 0;
        int status = ov_tensor_get_byte_size(ov_tensor_t, ref size);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return size;
      }
    }

    /// <summary>
    /// retrieve the pointer to the unmanaged data of the tensor.
    /// </summary>
    public IntPtr Data {
      get {
        IntPtr data = IntPtr.Zero;
        int status = ov_tensor_data(ov_tensor_t, ref data);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return data;
      }
    }

    /// <summary>
    /// retrieve the dimensions of the tensor.
    /// </summary>
    public long[] Dimensions {
      get {
        IntPtr shape_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(OvShape)));
        int status = ov_tensor_get_shape(ov_tensor_t, shape_ptr);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        OvShape shape = Marshal.PtrToStructure<OvShape>(shape_ptr);
        long[] Dims = new long[shape.rank];
        Marshal.Copy(shape.dims, Dims, 0, (int)shape.rank);
        Marshal.FreeHGlobal(shape_ptr);

        return Dims;
      }
    }

    /// <summary>
    /// retrieve the element type of the tensor.
    /// </summary>
    public OvElementType Type {
      get {
        int type = 0;
        int status = ov_tensor_get_element_type(ov_tensor_t, ref type);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return (OvElementType)type;
      }
    }

    internal IntPtr TensorPtr => ov_tensor_t;

    /// <summary>
    /// convert the tensor to a string representation for display
    /// </summary>
    /// <returns>tensor stringified</returns>
    public override string ToString() => $"[{string.Join(",", Dimensions)}], {Type}, {SizeBytes} bytes, @{Data.ToString("X16")}";

    public void Dispose() {
      ov_tensor_free(ov_tensor_t);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_tensor_free(IntPtr ov_tensor_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_create(int element_type, IntPtr ov_shape_t, ref IntPtr ov_tensor_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_get_size(IntPtr ov_tensor_t, ref int size);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_get_byte_size(IntPtr ov_tensor_t, ref int size);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_data(IntPtr ov_tensor_t, ref IntPtr data);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_get_shape(IntPtr ov_tensor_t, IntPtr ov_shape_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_tensor_get_element_type(IntPtr ov_tensor_t, ref int type);
    #endregion
  }
}
