using System;
using System.Threading;

namespace Kolokvijum1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=========================================================");
            Console.WriteLine("  Industrial Processing System — Kolokvijum 1 demo");
            Console.WriteLine("=========================================================");

            try
            {
                ProcessingSystem system = new ProcessingSystem(TimeSpan.FromSeconds(20));
                EventSystem eventSystem = new EventSystem(system);

                system.JobCompleted += (job, result) =>
                    Console.WriteLine($"  [EVENT] COMPLETED {job.Type} {job.Id} -> {result}");

                system.JobFailed += (job, status) =>
                    Console.WriteLine($"  [EVENT] {status} {job.Type} {job.Id}");

                Console.WriteLine($"Workers: {system.WorkerCount}, MaxQueue: {system.MaxQueueSize}");
                Console.WriteLine($"Initial jobs from XML: {system.Jobs.Count}");

                // Scenario 1 - uspesni poslovi
                Console.WriteLine("\n>>> SCENARIO 1: all jobs succeed");
                var s1Handles = new[]
                {
                    system.Submit(new Job(JOBTYPE.PRIME, "numbers:5_000,threads:2", priority: 1)),
                    system.Submit(new Job(JOBTYPE.IO,    "delay:200",              priority: 1)),
                    system.Submit(new Job(JOBTYPE.IO,    "delay:500",              priority: 3)),
                };
                AwaitAllSafe(s1Handles, "S1");

                // Scenario 2 - timeout poslovi
                Console.WriteLine("\n>>> SCENARIO 2: jobs that timeout and get ABORTed");
                var s2Handles = new[]
                {
                    system.Submit(new Job(JOBTYPE.IO, "delay:200",   priority: 1)),
                    system.Submit(new Job(JOBTYPE.IO, "delay:5_000", priority: 1)), // ABORT
                };
                AwaitAllSafe(s2Handles, "S2");

                // Scenario 3 - producer niti
                Console.WriteLine("\n>>> SCENARIO 3: multi-threaded producers");
                Thread[] producers = new Thread[system.WorkerCount];
                Random rand = new Random();

                for (int i = 0; i < producers.Length; i++)
                {
                    int pid = i;
                    producers[i] = new Thread(() =>
                    {
                        Random localRand = new Random(pid * 7919 + Environment.TickCount);
                        for (int k = 0; k < 5; k++)
                        {
                            try
                            {
                                Job job;
                                if (localRand.Next(2) == 0)
                                {
                                    int limit = localRand.Next(1000, 8000);
                                    job = new Job(JOBTYPE.PRIME,
                                        $"numbers:{limit},threads:{localRand.Next(1, 4)}",
                                        priority: localRand.Next(1, 6));
                                }
                                else
                                {
                                    job = new Job(JOBTYPE.IO,
                                        $"delay:{localRand.Next(50, 800)}",
                                        priority: localRand.Next(1, 6));
                                }

                                JobHandle h = system.Submit(job);
                                if (h == null)
                                    Console.WriteLine($"  [Producer-{pid}] Queue full, job rejected.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  [Producer-{pid}] Error: {ex.Message}");
                            }
                            Thread.Sleep(localRand.Next(50, 200));
                        }
                    })
                    { IsBackground = true };
                    producers[i].Start();
                }

                foreach (var p in producers) p.Join();
                Console.WriteLine("All producers done.");

                // GetTopJobs demo
                Console.WriteLine("\n>>> GetTopJobs(3):");
                foreach (var j in system.GetTopJobs(3))
                    Console.WriteLine($"     - {j}");

                // Cekaj da se red isprazni
                Console.WriteLine("\nWaiting for queue to drain...");
                while (system.PendingCount > 0)
                    Thread.Sleep(500);

                // Rucno generisi izvestaj
                Console.WriteLine("\n>>> Final report:");
                string report = system.Reports.GenerateReport();
                Console.WriteLine($"Saved to: {report}");

                Console.WriteLine("\nDone. Press Enter to exit.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }

        private static void AwaitAllSafe(JobHandle[] handles, string tag)
        {
            foreach (var h in handles)
            {
                if (h == null) { Console.WriteLine($"  [{tag}] Rejected."); continue; }
                try
                {
                    int r = h.Result.GetAwaiter().GetResult();
                    Console.WriteLine($"  [{tag}] {h.Id} -> {r}");
                }
                catch (JobAbortedException jex)
                {
                    Console.WriteLine($"  [{tag}] {h.Id} ABORTED: {jex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [{tag}] {h.Id} ERROR: {ex.Message}");
                }
            }
        }
    }
}