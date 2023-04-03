using CSharpOpenVinoV2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTestNuma {
  class Program {

    // create a static OvCore for the application
    static OvCore core = new OvCore();

    // model's name
    const string model_xml = "model.xml";

    // create a concurrent dictionary for the threads to store their logs and display them in the console grouped by thread
    static ConcurrentDictionary<int, string> threadsLogs = new ConcurrentDictionary<int, string>();

    static void Main(string[] args) {

      // let's look at the NUMA assignment for the main thread
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Model: {model_xml}");
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Application: {Numa.GetThreadInformation()}");

      // retrieve the number of NUMA nodes
      int nb_numa = Numa.GetNumaNumbers();

      // allocate the threads'array
      Thread[] threads = new Thread[nb_numa];

      // set properties (surprisingly AFFINITY set to CORE is changed to NUMA :) )
      core.SetProperty("PERFORMANCE_HINT", "LATENCY");
      core.SetProperty("AFFINITY", "CORE"); // CORE selected which will be changed
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} - PERFORMANCE_HINT: {core.GetProperty("PERFORMANCE_HINT")}");
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} - AFFINITY        : {core.GetProperty("AFFINITY")}");

      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Launch {nb_numa} threads (1 per NUMA node) OpenVINO mode");
      Console.WriteLine();

      #region second test with model ...
      for (int numa = 0; numa < nb_numa; numa++) {
        threads[numa] = new Thread(new ParameterizedThreadStart(AssignedNumaInference));
        threads[numa].SetApartmentState(ApartmentState.STA);
        threads[numa].Priority = ThreadPriority.Normal;
        threads[numa].Start(numa);
        Thread.Sleep(1000); // we add a 1s delay in the start to make sure that tensors are not allocated simultaneously from both threads
      }

      // wait for the threads to finish and display their logs on the console
      for (int numa = 0; numa < nb_numa; numa++) {
        threads[numa].Join();
        Console.WriteLine(threadsLogs[numa]);
        Console.WriteLine();
      }
      #endregion
    }

    #region openvino inference thread, dispatch inference that should contained into the NUMA node of the thread
    static void AssignedNumaInference(object numa_node) {

      // retrieve the numa node of this thread
      int numa = (int)numa_node;
      // assign this thread to the NUMA node
      Numa.SetThreadAffinities(numa);

      // the stringbuilder will be used to log
      StringBuilder sb = new StringBuilder();

      sb.AppendLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Thread[{numa}] on start: {Numa.GetThreadInformation()}");

      // allocate all required objects to run inferences
      using (var model = core.CompileModelFromFile(model_xml, "CPU"))
      using (var infer = model.CreateInferRequest())
      using (var input0 = infer.GetInputTensorByIndex(0))
      using (var output0 = infer.GetOutputTensorByIndex(0)) {

        // warm-up inference (physical object allocation)
        infer.Infer();
        // display input and output tensor numa's location
        sb.AppendLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Thread[{numa}]: Input Tensor" + Environment.NewLine + Numa.GetMemoryMappingInfos(input0.Data, (uint)input0.SizeBytes, "            "));
        sb.AppendLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Thread[{numa}]: Output Tensor" + Environment.NewLine + Numa.GetMemoryMappingInfos(output0.Data, (uint)output0.SizeBytes, "            "));

        // let's create some allocations through Marshal as reference:
        IntPtr testMemory = Marshal.AllocHGlobal(input0.SizeBytes);
        FillMemory(testMemory, (uint)input0.SizeBytes, 0); // force allocation
          
        // display test memory numa's location
        sb.AppendLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Thread[{numa}]: Marshal Test Memory" + Environment.NewLine + Numa.GetMemoryMappingInfos(testMemory, (uint)input0.SizeBytes, "            "));

        Marshal.FreeHGlobal(testMemory);

        // start a stopwatch to have bench based on duration
        var sw = new Stopwatch();
        sw.Restart();

        // loop until 10s duration is reached
        while (sw.ElapsedMilliseconds < 10000) {
          infer.Infer();
        }

        // log the NUMA node before exit to observe any changes
        sb.Append($"{DateTime.Now.ToString("HH:mm:ss.ff")} Thread[{numa}] on exit: {Numa.GetThreadInformation()}");
        threadsLogs.AddOrUpdate(numa, sb.ToString(), (key, old) => sb.ToString());
      }
    }
    #endregion

    // import method to clean unmanaged memory and force its allocation
    [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
    static extern void FillMemory(IntPtr destination, uint length, byte fill);
  }
}
