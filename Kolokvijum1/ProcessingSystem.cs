using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kolokvijum1
{
    public class ProcessingSystem
    {
        // Polja
        public int WorkerCount { get; private set; }
        public int MaxQueueSize { get; private set; }
        public SortedSet<Job> Jobs { get; private set; }

        // Events
        public event Action<Job, int> JobCompleted;
        public event Action<Job, string> JobFailed; // string = "FAILED" ili "ABORT"

        // Idempotency
        private readonly HashSet<Guid> executedJobIds = new HashSet<Guid>();

        // lock, niti, pomocne kolekcije
        private readonly object _lock = new object();
        private readonly Thread[] radnici;
        private readonly Random random = new Random();
        private readonly Dictionary<Guid, JobHandle> jobHandles = new Dictionary<Guid, JobHandle>();
        private readonly Dictionary<Guid, Job> jobsById = new Dictionary<Guid, Job>();

        // Report generator
        public ReportGenerator Reports { get; private set; }


        // Constructor for testing - no XML needed
        public ProcessingSystem(int workerCount, int maxQueueSize)
        {
            WorkerCount = workerCount;
            MaxQueueSize = maxQueueSize;

            Jobs = new SortedSet<Job>(Comparer<Job>.Create((a, b) =>
                a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : a.Id.CompareTo(b.Id)));

            Reports = new ReportGenerator("reports_test");

            radnici = new Thread[WorkerCount];
            for (int i = 0; i < WorkerCount; i++)
            {
                radnici[i] = new Thread(WorkerLoop) { IsBackground = true };
                radnici[i].Start();
            }

            lock (_lock) { Monitor.PulseAll(_lock); }
        }
        public ProcessingSystem(TimeSpan? reportInterval = null)
        {
            XElement sistem = XElement.Load(ResolveConfig());
            WorkerCount = (int)sistem.Element("WorkerCount");
            MaxQueueSize = (int)sistem.Element("MaxQueueSize");

            Jobs = new SortedSet<Job>(Comparer<Job>.Create((a, b) =>
                a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : a.Id.CompareTo(b.Id)));

            // Ucitaj pocetne jobove iz XML-a
            foreach (var j in sistem.Element("Jobs").Elements("Job"))
            {
                var job = new Job(
                    Helpers.ConvertStringToJobType((string)j.Attribute("Type")),
                    (string)j.Attribute("Payload"),
                    (int)j.Attribute("Priority")
                );
                Jobs.Add(job);
                jobsById[job.Id] = job;
                jobHandles[job.Id] = new JobHandle(job.Id);
            }

            Reports = new ReportGenerator("reports", reportInterval);

            // Pokreni worker niti
            radnici = new Thread[WorkerCount];
            for (int i = 0; i < WorkerCount; i++)
            {
                radnici[i] = new Thread(WorkerLoop) { IsBackground = true };
                radnici[i].Start();
            }

            lock (_lock) { Monitor.PulseAll(_lock); }
        }

        private string ResolveConfig()
        {
            string[] candidates = {
        "SystemConfig.xml",
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemConfig.xml"),
        Path.Combine("..", "..", "SystemConfig.xml"),
        Path.Combine("..", "..", "..", "Kolokvijum1", "SystemConfig.xml"), // from test project
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Kolokvijum1", "SystemConfig.xml")
    };
            foreach (var p in candidates)
                if (File.Exists(p)) return p;
            throw new FileNotFoundException("SystemConfig.xml not found");
        }

        private void WorkerLoop()
        {
            while (true)
            {
                Job job = null;
                lock (_lock)
                {
                    while (Jobs.Count == 0)
                        Monitor.Wait(_lock);
                    job = Jobs.Min;
                    Jobs.Remove(job);
                }

                try { ExecuteWithRetry(job); }
                catch (Exception ex) { Console.WriteLine($"[Worker] Error: {ex.Message}"); }
            }
        }

        private void ExecuteWithRetry(Job job)
        {
            Exception lastError = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    var task = Task.Run(() => ProcessJob(job));
                    bool finished = task.Wait(2000);
                    sw.Stop();

                    if (!finished)
                        throw new TimeoutException($"Job timed out on attempt {attempt}.");

                    int result = task.Result;
                    sw.Stop();

                    lock (_lock)
                    {
                        executedJobIds.Add(job.Id);
                        if (jobHandles.TryGetValue(job.Id, out JobHandle handle))
                        {
                            handle.Complete(result);
                            jobHandles.Remove(job.Id);
                        }
                    }

                    JobCompleted?.Invoke(job, result);
                    Reports.Record(new ExecutionRecord
                    {
                        JobId = job.Id,
                        Type = job.Type,
                        Success = true,
                        DurationMs = sw.Elapsed.TotalMilliseconds
                    });
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex is AggregateException aex ? aex.InnerException : ex;
                    Console.WriteLine($"[Worker] Job {job.Id} attempt {attempt}/3 failed: {lastError.Message}");

                    if (attempt < 3)
                        JobFailed?.Invoke(job, "FAILED");
                }
            }

            // Sva 3 pokusaja propala - ABORT
            JobFailed?.Invoke(job, "ABORT");
            lock (_lock)
            {
                if (jobHandles.TryGetValue(job.Id, out JobHandle h))
                {
                    h.Fail(new JobAbortedException(job.Id, lastError?.Message));
                    jobHandles.Remove(job.Id);
                }
            }
            Reports.Record(new ExecutionRecord { JobId = job.Id, Type = job.Type, Success = false, DurationMs = 0 });
        }

        private int ProcessJob(Job job)
        {
            // Idempotency check
            lock (_lock)
            {
                if (executedJobIds.Contains(job.Id))
                {
                    Console.WriteLine($"Job {job.Id} already executed, skipping.");
                    return -1;
                }

            }

            int result = 0;

            if (job.Type == JOBTYPE.PRIME) result = ProcessPrimeJob(job.Payload);
            else if (job.Type == JOBTYPE.IO) result = ProcessIOJob(job.Payload);
            return result;
        }

        private int ProcessPrimeJob(string payload)
        {
            var parts = payload.Split(',');
            int limit = int.Parse(parts[0].Split(':')[1].Replace("_", ""));
            int threadCount = Math.Max(1, Math.Min(8, int.Parse(parts[1].Split(':')[1].Replace("_", ""))));

            int primeCount = 0;
            object countLock = new object();
            int rangeSize = limit / threadCount;
            Thread[] threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int start = i * rangeSize + 2;
                int end = (i == threadCount - 1) ? limit : start + rangeSize - 1;

                threads[i] = new Thread(() =>
                {
                    int local = 0;
                    for (int n = start; n <= end; n++)
                        if (IsPrime(n)) local++;
                    lock (countLock) { primeCount += local; }
                });
                threads[i].Start();
            }

            foreach (var t in threads) t.Join();
            return primeCount;
        }

        private bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;
            for (int i = 3; i * i <= n; i += 2)
                if (n % i == 0) return false;
            return true;
        }

        private int ProcessIOJob(string payload)
        {
            int delay = int.Parse(payload.Split(':')[1].Replace("_", ""));
            Thread.Sleep(delay);
            return random.Next(0, 101);
        }

        public JobHandle Submit(Job job)
        {
            lock (_lock)
            {
                if (Jobs.Count >= MaxQueueSize)
                {
                    Console.WriteLine($"Queue full! Job {job.Id} rejected.");
                    return null;
                }
                var handle = new JobHandle(job.Id);
                jobHandles[job.Id] = handle;
                jobsById[job.Id] = job;
                Jobs.Add(job);
                Monitor.Pulse(_lock);
                return handle;
            }
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            lock (_lock) { return Jobs.Take(n).ToList(); }
        }

        public Job GetJob(Guid id)
        {
            lock (_lock) { return jobsById.TryGetValue(id, out Job j) ? j : null; }
        }

        public int PendingCount
        {
            get { lock (_lock) { return Jobs.Count; } }
        }
    }
}