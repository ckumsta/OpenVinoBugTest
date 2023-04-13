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
    static OvCompiledModel compiledModel = null;

    // model's name
    const string ModelXmlPath = "model.xml";

    // number of threads of inference per NUMA node
    const int NbThreadsPerNuma = 4;

    // test duration in milliseconds
    const long Duration_ms = 10_000;

    static void Main(string[] args) {

      // let's look at the NUMA assignment for the main thread
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Model: {ModelXmlPath}");
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Application: {Numa.GetThreadInformation()}");

      // retrieve the number of NUMA nodes
      int nb_numa = Numa.GetNumaNumbers();

      // compile the model with the following properties:
      //      PERFORMANCE_HINT : LATENCY
      //              AFFINITY : NUMA
      // INFERENCE_NUM_THREADS : 8

      // if correctly understood, that will create 8 inference inputs (4 per NUMA node).
      compiledModel = core.CompileModelFromFile(ModelXmlPath, 8, "CPU");

      // allocate the threads'array
      Thread[] threads = new Thread[nb_numa];
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Launch {nb_numa} threads (1 per NUMA node)");
      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Starting inferences for {Duration_ms / 1000.0:F2} secondes ...");

      // create 1 main thread per numa node
      for (int numa = 0; numa < nb_numa; numa++) {
        threads[numa] = new Thread(new ParameterizedThreadStart(AssignedNumaInference));
        threads[numa].SetApartmentState(ApartmentState.STA);
        threads[numa].Priority = ThreadPriority.Normal;
        threads[numa].Start(numa);
      }

      // wait for the threads to finish and display their logs on the console
      foreach (var thread in threads)
        thread.Join();

      Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ff")} Inferences done.");

      // dispose all allocated objects
      compiledModel.Dispose();
      core.Dispose();
    }

    #region openvino inference thread, dispatch inference that should contained into the NUMA node of the thread
    static void AssignedNumaInference(object numa_node) {

      // retrieve the numa node of this thread
      int numa = (int)numa_node;

      // assign this thread to the NUMA node
      Numa.SetThreadAffinities(numa);

      // create NbThreadsPerNuma inference streams for this numa node
      var inferRequests = Enumerable
          .Range(0, NbThreadsPerNuma)
          .Select(i => compiledModel.CreateInferRequest())
          .ToArray();

      // start a stopwatch to have bench based on duration
      var sw = new Stopwatch();
      sw.Restart();

      // loop until Duration_ms duration is reached
      while (sw.ElapsedMilliseconds < Duration_ms) {
        // execute parallel infer request inferences
        Parallel.ForEach(inferRequests, (infer_request) => infer_request.Infer());
      }

      // release objects InferRequest
      foreach (var ir in inferRequests)
        ir.Dispose();

    }
    #endregion
  }
}
