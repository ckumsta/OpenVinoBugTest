using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public sealed class OvInferRequest: IDisposable {

    /// <summary>
    /// internal representation of unmanaged infer request object.
    /// </summary>
    private readonly IntPtr ov_infer_request_t;

    /// <summary>
    /// constructor based on unmanaged infer request object.
    /// </summary>
    /// <param name="ov_infer_request_t">pointer to the infer request unmanaged object</param>
    public OvInferRequest(IntPtr ov_infer_request_t) {
      this.ov_infer_request_t = ov_infer_request_t;
    }

    /// <summary>
    /// assign a tensor to the inputs by index.
    /// </summary>
    /// <param name="index">index of input</param>
    /// <param name="tensor">tensor to use as input</param>
    public void SetInputTensorByIndex(int index, OvTensor tensor) {
      int status = ov_infer_request_set_input_tensor_by_index(ov_infer_request_t, index, tensor.TensorPtr);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// retrieve an output tensor by index.
    /// </summary>
    /// <param name="index">index of output</param>
    /// <returns>tensor from this output index</returns>
    public OvTensor GetOutputTensorByIndex(int index) {
      IntPtr ov_tensor_t = IntPtr.Zero;
      int status = ov_infer_request_get_output_tensor_by_index(ov_infer_request_t, index, ref ov_tensor_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      return new OvTensor(ov_tensor_t);
    }

    public OvTensor GetInputTensorByIndex(int index) {
      IntPtr ov_tensor_t = IntPtr.Zero;
      int status = ov_infer_request_get_input_tensor_by_index(ov_infer_request_t, index, ref ov_tensor_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      return new OvTensor(ov_tensor_t);
    }

    /// <summary>
    /// launch inference (blocking function).
    /// </summary>
    public void Infer() {
      int status = ov_infer_request_infer(ov_infer_request_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// launch asynchronous inference.
    /// </summary>
    public void InferAsyncStart() {
      int status = ov_infer_request_start_async(ov_infer_request_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// cancel an asynchronous inference.
    /// </summary>
    public void InferAsyncCancel() {
      int status = ov_infer_request_cancel(ov_infer_request_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// wait the end of an asynchronous inference.
    /// </summary>
    public void InferAsyncWait() {
      int status = ov_infer_request_wait(ov_infer_request_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// destructor of the object.
    /// </summary>
    public void Dispose() {
      ov_infer_request_free(ov_infer_request_t);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_infer_request_free(IntPtr infer);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)] 
    private static extern int ov_infer_request_set_input_tensor_by_index(IntPtr infer, int input_index, IntPtr ov_tensor_ptr);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_get_output_tensor_by_index(IntPtr infer, int output_index, ref IntPtr ov_tensor_ptr);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_get_input_tensor_by_index(IntPtr infer, int input_index, ref IntPtr ov_tensor_ptr);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_infer(IntPtr infer);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_start_async(IntPtr infer);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_cancel(IntPtr infer);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_infer_request_wait(IntPtr infer);

    #endregion

  }
}
