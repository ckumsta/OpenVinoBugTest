using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public sealed class OvCompiledModel : IDisposable {

    /// <summary>
    /// internal pointer to the unmanaged compiled model.
    /// </summary>
    private readonly IntPtr ov_compiled_model_t;

    /// <summary>
    /// Constructor from created unmanaged compiled model.
    /// </summary>
    /// <param name="ov_compiled_model_t">unmanaged compiled model</param>
    public OvCompiledModel(IntPtr ov_compiled_model_t) {
      this.ov_compiled_model_t = ov_compiled_model_t;
    }

    /// <summary>
    /// Retrieve an input port description by input index.
    /// </summary>
    /// <param name="index">index of input</param>
    /// <returns>Input description object</returns>
    public OvOutputPort GetInputPortByIndex(int index) {
      IntPtr ov_output_const_port_t = IntPtr.Zero;
      int status = ov_compiled_model_input_by_index(ov_compiled_model_t, index, ref ov_output_const_port_t);
      // check if all ok
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      return new OvOutputPort(ov_output_const_port_t);
    }

    /// <summary>
    /// Retrieve an output port description by output index.
    /// </summary>
    /// <param name="index">index of output</param>
    /// <returns>Output description object</returns>
    public OvOutputPort GetOutputPortByIndex(int index) {
      IntPtr ov_output_const_port_t = IntPtr.Zero;
      int status = ov_compiled_model_output_by_index(ov_compiled_model_t, index, ref ov_output_const_port_t);
      // check if all ok
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      return new OvOutputPort(ov_output_const_port_t);
    }

    /// <summary>
    /// Retrieve an inference request object from the compiled object.
    /// This inference request will be used to compute inferences.
    /// </summary>
    /// <returns>inference request object</returns>
    public OvInferRequest CreateInferRequest() {
      IntPtr infer_request = IntPtr.Zero;
      int status = ov_compiled_model_create_infer_request(ov_compiled_model_t, ref infer_request);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      return new OvInferRequest(infer_request);
    }

    public string GetProperty(string property_key) {
      IntPtr property_key_str = Marshal.StringToHGlobalAnsi(property_key);
      IntPtr property_value_str = IntPtr.Zero;
      int status = ov_compiled_model_get_property(ov_compiled_model_t, property_key_str, ref property_value_str);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      string property_value = Marshal.PtrToStringAnsi(property_value_str);
      Marshal.FreeHGlobal(property_key_str);
      return property_value;
    }

    public void SetProperty(string property_key, string property_value) {
      IntPtr property_key_str = Marshal.StringToHGlobalAnsi(property_key);
      IntPtr property_value_str = Marshal.StringToHGlobalAnsi(property_value);
      int status = ov_compiled_model_set_property(ov_compiled_model_t, property_key_str, property_value_str);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      Marshal.FreeHGlobal(property_value_str);
      Marshal.FreeHGlobal(property_key_str);
    }

    /// <summary>
    /// Number of inputs for the model.
    /// </summary>
    public int InputsSize {
      get {
        int input_size = 0;
        int status = ov_compiled_model_inputs_size(ov_compiled_model_t, ref input_size);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return input_size;
      }
    }

    /// <summary>
    /// Number of output for the model.
    /// </summary>
    public int OutputsSize {
      get {
        int output_size = 0;
        int status = ov_compiled_model_outputs_size(ov_compiled_model_t, ref output_size);
        if (status != 0) {
          var msg = OvError.GetErrorFromCode(status);
          throw new Exception(msg);
        }
        return output_size;
      }
    }

    /// <summary>
    /// Friendly name for the model.
    /// </summary>
    public string Name {
      get {
        string name;
        IntPtr ov_model_t = IntPtr.Zero;
        int status = ov_compiled_model_get_runtime_model(ov_compiled_model_t, ref ov_model_t);
        using (var model = new OvModel(ov_model_t)) {
          name = model.Name;
        }
        return name;
      }
    }

    /// <summary>
    /// summarize the de model if used as a string.
    /// </summary>
    /// <returns>string representing the model</returns>
    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"Model {Name}");
      for (int i = 0; i < InputsSize; i++)
        using (var port = GetInputPortByIndex(i)) {
          sb.AppendLine($" - Input[{i}] : {port}");
        }
      for (int i = 0; i < OutputsSize; i++)
        using (var port = GetOutputPortByIndex(i)) {
          sb.AppendLine($" - Output[{i}]: {port}");
        }
      return sb.ToString();
    }

    /// <summary>
    /// destroy all resources.
    /// </summary>
    public void Dispose() {
      ov_compiled_model_free(ov_compiled_model_t);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_compiled_model_free(IntPtr ov_compiled_model_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_inputs_size(IntPtr ov_compiled_model_t, ref int input_size);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_outputs_size(IntPtr ov_compiled_model_t, ref int output_size);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_input_by_index(IntPtr ov_compiled_model_t, int index, ref IntPtr ov_output_const_port_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_output_by_index(IntPtr ov_compiled_model_t, int index, ref IntPtr ov_output_const_port_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_create_infer_request(IntPtr ov_compiled_model_t, ref IntPtr ov_infer_request_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_get_runtime_model(IntPtr ov_compiled_model_t, ref IntPtr ov_model_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_get_property(IntPtr ov_compiled_model_t, IntPtr property_key_str, ref IntPtr property_value_str);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_compiled_model_set_property(IntPtr ov_compiled_model_t, IntPtr property_key, IntPtr property_value);

    #endregion
  }
}
