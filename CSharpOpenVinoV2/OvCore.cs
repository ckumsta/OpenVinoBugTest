using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public sealed class OvCore : IDisposable {

    /// <summary>
    /// internal pointer to the unmanaged core object.
    /// </summary>
    private readonly IntPtr ov_core_t;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OvCore() {
      int status = ov_core_create(ref ov_core_t);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
    }

    /// <summary>
    /// Destructor.
    /// </summary>
    public void Dispose() {
      ov_core_free(ov_core_t);
    }

    /// <summary>
    /// Create a compiled model from the file .xml (and associated .bin).
    /// </summary>
    /// <param name="xml_path">path to xml file (.bin should have the same location)</param>
    /// <param name="device">device used for this model</param>
    /// <returns>a compile model object</returns>
    public OvCompiledModel CompileModelFromFile(string xml_path, string device = "CPU") {
      // convert strings to unmanaged strings
      IntPtr str_xml = Marshal.StringToHGlobalAnsi(xml_path);
      // convert device to unmanaged string
      IntPtr str_device = Marshal.StringToHGlobalAnsi(device);
      // retrieve a model object from that
      IntPtr model = IntPtr.Zero;
      int status = ov_core_compile_model_from_file(ov_core_t, str_xml, str_device, 0, ref model);
      // check if all ok
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      Marshal.FreeHGlobal(str_device);
      Marshal.FreeHGlobal(str_xml);
      // return a boxed object of model
      return new OvCompiledModel(model);
    }

    /// <summary>
    /// get property of OpenVINO.
    /// </summary>
    /// <param name="property_name">name of the property</param>
    /// <param name="device">device targetted by the property</param>
    /// <returns>value of the property</returns>
    public string GetProperty(string property_name, string device = "CPU") {
      IntPtr device_str = Marshal.StringToHGlobalAnsi(device);
      IntPtr property_name_str = Marshal.StringToHGlobalAnsi(property_name);
      IntPtr property_value_str = IntPtr.Zero;
      int status = ov_core_get_property(ov_core_t, device_str, property_name_str, ref property_value_str);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      string property_value = Marshal.PtrToStringAnsi(property_value_str);
      Marshal.FreeHGlobal(property_name_str);
      Marshal.FreeHGlobal(device_str);
      return property_value;
    }

    /// <summary>
    /// set property of OpenVINO.
    /// </summary>
    /// <param name="property_name">name of the property</param>
    /// <param name="property_value">device targetted by the property</param>
    /// <param name="device">value of the property</param>
    public void SetProperty(string property_name, string property_value, string device = "CPU") {
      IntPtr device_str = Marshal.StringToHGlobalAnsi(device);
      IntPtr property_name_str = Marshal.StringToHGlobalAnsi(property_name);
      IntPtr property_value_str = Marshal.StringToHGlobalAnsi(property_value);
      int status = ov_core_set_property(ov_core_t, device_str, property_name_str, property_value_str);
      if (status != 0) {
        var msg = OvError.GetErrorFromCode(status);
        throw new Exception(msg);
      }
      Marshal.FreeHGlobal(property_value_str);
      Marshal.FreeHGlobal(property_name_str);
      Marshal.FreeHGlobal(device_str);
    }

    #region external calls
    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_core_create(ref IntPtr ov_core_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ov_core_free(IntPtr ov_core_t);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_core_read_model(IntPtr ov_core_t, IntPtr str_xml, IntPtr str_bin, ref IntPtr model);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_core_compile_model_from_file(IntPtr ov_core_t, IntPtr str_xml, IntPtr str_device_name, int prop_args_size, ref IntPtr compiled_model);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_core_get_property(IntPtr ov_core_t, IntPtr device_str, IntPtr property_name_str, ref IntPtr property_value_str);

    [DllImport("openvino_c.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ov_core_set_property(IntPtr ov_core_t, IntPtr device_str, IntPtr property_name_str, IntPtr property_value_str);
    #endregion
  }
}
