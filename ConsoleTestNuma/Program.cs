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

    // Create a single instance of Compiled Model
    static OvCompiledModel compiledModel;

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

      // compile the model with the following properties:
      //      PERFORMANCE_HINT : LATENCY
      //              AFFINITY : NUMA
      // INFERENCE_NUM_THREADS : 8

      // if correctly understood, that will create 8 inference inputs (4 per NUMA node).
      compiledModel = core.CompileModelFromFile(model_xml, 8, "CPU");

      // allocate the threads'array
      Thread[] threads = new Thread[nb_numa];
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

      compiledModel.Dispose();
      core.Dispose();
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

      // create 4 inference streams for this numa node
      var inferRequests = new OvInferRequest[] {
        compiledModel.CreateInferRequest(),
        compiledModel.CreateInferRequest(),
        compiledModel.CreateInferRequest(),
        compiledModel.CreateInferRequest()
      };

      {
        // start a stopwatch to have bench based on duration
        var sw = new Stopwatch();
        sw.Restart();

        // loop until 10s duration is reached
        while (sw.ElapsedMilliseconds < 10000) {
          Parallel.Invoke(
            () => inferRequests[0].Infer(),
            () => inferRequests[1].Infer(),
            () => inferRequests[2].Infer(),
            () => inferRequests[3].Infer());
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
