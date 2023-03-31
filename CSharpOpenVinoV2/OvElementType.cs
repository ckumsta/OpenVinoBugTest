using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  /// <summary>
  /// Enumeration of all available types.
  /// </summary>
  public enum OvElementType : uint {
    UNDEFINED = 0,
    DYNAMIC,
    BOOLEAN,
    BF16,
    F16,
    F32,
    F64,
    I4,
    I8,
    I16,
    I32,
    I64,
    U1,
    U4,
    U8,
    U16,
    U32,
    U64,
  }
}
