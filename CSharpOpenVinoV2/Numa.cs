using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenVinoV2 {
  public static class Numa {
    [StructLayout(LayoutKind.Sequential)]
    private struct GROUP_AFFINITY_64 {
      public UInt64 Mask;
      public UInt16 Group;
      public UInt16 Reserved1;
      public UInt16 Reserved2;
      public UInt16 Reserved3;
    };

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern bool GetNumaNodeProcessorMaskEx(Byte Node, out GROUP_AFFINITY_64 GroupAffinity);

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESSOR_NUMBER {
      public UInt16 Group;
      public Byte Number;
      public Byte Reserved;
    }

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern void GetCurrentProcessorNumberEx(out PROCESSOR_NUMBER processorNumber);

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern bool GetNumaHighestNodeNumber(out UInt32 highestNode);

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentThread();

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern bool SetThreadGroupAffinity(IntPtr hwnThread, ref GROUP_AFFINITY_64 affinity_new, out GROUP_AFFINITY_64 affinity_prev);

    [DllImport(@"kernel32.dll", SetLastError = true)]
    private static extern bool GetNumaAvailableMemoryNodeEx(UInt16 node, out UInt64 memory);

    private static Byte[] NumaNodesHAL = null;
    private static void CreateNumaNodeHAL() {
      UInt32 highestNode;
      GetNumaHighestNodeNumber(out highestNode);
      List<Byte> lstNode = new List<Byte>();
      for (Byte n = 0; n <= highestNode; n++) {
        GROUP_AFFINITY_64 affinity;
        if (!GetNumaNodeProcessorMaskEx(n, out affinity)) {
          throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
        } else {
          if (affinity.Mask != 0) // there is some core active in this node ...
            lstNode.Add(n);
        }
      }
      NumaNodesHAL = lstNode.ToArray();
    }

    private static int[] NumaDispatchTemplate = null;
    public static void SetNumaDispatchTemplate(int[] template) {
      if (NumaNodesHAL == null) CreateNumaNodeHAL();
      // verify that all element in template are ok (within the nodes range)
      for (int i = 0; i < template.Length; i++) {
        if (template[i] < 0) template[i] = 0;
        template[i] = template[i] % NumaNodesHAL.Length;
      }
      NumaDispatchTemplate = template;
    }

    public static int GetNumaNumbers() {
      if (NumaNodesHAL == null) CreateNumaNodeHAL();
      return NumaNodesHAL.Length;
    }

    public static void GetThreadInformation(out int cpu, out int group, out int numa) {
      cpu = -1; group = -1; numa = -1;

      if (NumaNodesHAL == null) CreateNumaNodeHAL();

      PROCESSOR_NUMBER processorNumber;
      GetCurrentProcessorNumberEx(out processorNumber);
      for (Byte n = 0; n < NumaNodesHAL.Length; n++) {
        GROUP_AFFINITY_64 affinity;
        if (!GetNumaNodeProcessorMaskEx(n, out affinity)) {
          throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
        } else {
          if (affinity.Group == processorNumber.Group && ((affinity.Mask >> (int)processorNumber.Number) & 0x01) != 0) {
            numa = n;
            cpu = processorNumber.Number;
            group = processorNumber.Group;
          }
        }
      }
    }

    public static void SetThreadAffinities(int index) {
      if (NumaNodesHAL == null) CreateNumaNodeHAL();
      if (index < 0) index = 0;

      index = index % ((NumaDispatchTemplate == null) ? NumaNodesHAL.Length : NumaDispatchTemplate.Length);
      Byte numa = (NumaDispatchTemplate == null) ? NumaNodesHAL[index] : NumaNodesHAL[NumaDispatchTemplate[index]];

      GROUP_AFFINITY_64 affinity, previous;
      if (!GetNumaNodeProcessorMaskEx(numa, out affinity)) {
        throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
      }
      IntPtr thHandle = GetCurrentThread();
      if (!SetThreadGroupAffinity(thHandle, ref affinity, out previous)) {
        throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
      }
    }

    public static string GetThreadInformation() {
      int cpu, grp, numa;
      GetThreadInformation(out cpu, out grp, out numa);
      return "[Cpu=" + cpu + ",Grp=" + grp + ",Numa=" + numa + "]";
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_INFO {
      public UInt16 wProcessorArchitecture;
      public UInt16 wReserved;
      public UInt32 dwPageSize;
      public IntPtr lpMinimumApplicationAddress;
      public IntPtr lpMaximumApplicationAddress;
      public UInt64 dwActiveProcessorMask;
      public UInt32 dwNumberOfProcessors;
      public UInt32 dwProcessorType;
      public UInt32 dwAllocationGranularity;
      public UInt16 wProcessorLevel;
      public UInt16 wProcessorRevision;
    }
    [DllImport(@"kernel32.dll", SetLastError = true)]
    extern static void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
    [DllImport(@"kernel32.dll", SetLastError = true)]
    extern static IntPtr GetCurrentProcess();
    [DllImport(@"Psapi.dll", SetLastError = true)]
    extern static bool QueryWorkingSetEx(IntPtr hProcess, Int64[] pv, UInt32 cb);
    [DllImport(@"Kernel32.dll", SetLastError = true)]
    extern static UInt32 GetLastError();

    private static bool isMemoryCoherent(IntPtr HostAddress, uint length, uint numa_node) {
      #region Get Pages infos
      SYSTEM_INFO sysinfo;
      GetSystemInfo(out sysinfo);
      UInt32 PageSize = sysinfo.dwPageSize;

      Int64 StartPage = (HostAddress.ToInt64() / PageSize);
      Int64 EndPage = ((HostAddress.ToInt64() + length - 1) / PageSize);
      Int64 NumPages = (EndPage - StartPage);

      Int64 StartPtr = (PageSize * StartPage);
      #endregion

      #region get back per page system information
      Int64[] WsInfo = new Int64[NumPages * 2];
      UInt32 WsInfoSize = (UInt32)NumPages * (8 + 8);

      for (int i = 0; i < NumPages; i++)
        WsInfo[i * 2] = (StartPage + i) * PageSize;

      if (!QueryWorkingSetEx(GetCurrentProcess(), WsInfo, WsInfoSize)) {
        throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
      }
      #endregion

      for (UInt32 i = 0; i < NumPages; i++) {
        UInt32 Node = getNodeInfoFromVirtualAttribute(WsInfo[i * 2 + 1]);
        if (Node != numa_node) return false;
      }

      // ok we got everything right
      return true;
    }

    public static string GetMemoryMappingInfos(IntPtr HostAddress, uint length, string offset="") {
      List<string> debug = new List<string>();

      #region Get Pages infos
      SYSTEM_INFO sysinfo;
      GetSystemInfo(out sysinfo);
      UInt32 PageSize = sysinfo.dwPageSize;

      Int64 StartPage = (HostAddress.ToInt64() / PageSize);
      Int64 EndPage = ((HostAddress.ToInt64() + length - 1) / PageSize);
      Int64 NumPages = (EndPage - StartPage);

      Int64 StartPtr = (PageSize * StartPage);
      #endregion

      #region get back per page system information
      Int64[] WsInfo = new Int64[NumPages * 2];
      UInt32 WsInfoSize = (UInt32)NumPages * (8 + 8);

      for (int i = 0; i < NumPages; i++)
        WsInfo[i * 2] = (StartPage + i) * PageSize;

      if (!QueryWorkingSetEx(GetCurrentProcess(), WsInfo, WsInfoSize)) {
        throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
      }
      #endregion

      #region create debug log per group of similar pages
      Int64 RegionStart = 0;
      bool RegionIsValid = false;
      UInt32 RegionNode = 0;

      for (UInt32 i = 0; i < NumPages; i++) {
        Int64 Address = WsInfo[i * 2 + 0];
        bool IsValid = getValidInfoFromVirtualAttribute(WsInfo[i * 2 + 1]);
        UInt32 Node = getNodeInfoFromVirtualAttribute(WsInfo[i * 2 + 1]);

        if (i == 0) {
          RegionStart = Address;
          RegionIsValid = IsValid;
          RegionNode = Node;
        }

        if (IsValid != RegionIsValid || Node != RegionNode) {
          if (RegionIsValid)
            debug.Add(
                offset + "[" + RegionStart.ToString("X").PadLeft(8) + "-" + (Address - 1).ToString("X").PadLeft(8) + "] " +
                "(" + ((Address - RegionStart) / PageSize).ToString("000000") + " pages) -> Node=" + RegionNode);
          else
            debug.Add(
                offset + "[" + RegionStart.ToString("X").PadLeft(8) + "-" + (Address - 1).ToString("X").PadLeft(8) + "] " +
                "(" + ((Address - RegionStart) / PageSize).ToString("000000") + " pages) -> No valid pages");

          RegionStart = Address;
          RegionIsValid = IsValid;
          RegionNode = Node;
        }

        if (i == (NumPages - 1)) {
          if (RegionIsValid)
            debug.Add(
                offset + "[" + RegionStart.ToString("X").PadLeft(8) + "-" + (Address + PageSize - 1).ToString("X").PadLeft(8) + "] " +
                "(" + (((Address - RegionStart) / PageSize) + 1).ToString("000000") + " pages) -> Node=" + RegionNode);
          else
            debug.Add(
                offset + "[" + RegionStart.ToString("X").PadLeft(8) + "-" + (Address + PageSize - 1).ToString("X").PadLeft(8) + "] " +
                "(" + (((Address - RegionStart) / PageSize) + 1).ToString("000000") + " pages) -> No valid pages");
        }
      }
      #endregion

      return string.Join(Environment.NewLine, debug);
    }

    private static bool getValidInfoFromVirtualAttribute(Int64 VirtualAttribute) {
      return ((VirtualAttribute & 0x00000001) == 0x00000001);
    }

    private static UInt32 getNodeInfoFromVirtualAttribute(Int64 VirtualAttribute) {
      return (UInt32)((VirtualAttribute >> 16) & 0x0000003F);
    }

    public static UInt64 GetNumaAvailableMemory(UInt16 node) {
      UInt64 memory;
      if (!GetNumaAvailableMemoryNodeEx(node, out memory)) {
        throw new Exception(new Win32Exception(Marshal.GetLastWin32Error()).Message);
      }
      return memory;
    }
  }
}
