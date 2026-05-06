using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kolokvijum1
{
    public class ProcessingSystem
    {
        // Polja
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public SortedSet<Job> Jobs { get; set; }

        // Dogadjaji
        public event Action<Job, int> JobCompleted;
        public event Action<Job, string> JobFailed;

        // Idempotency
        private readonly HashSet<Guid> executedJobIds = new HashSet<Guid>();

        // lock i niti
        private readonly object _lock = new object();
        private readonly Thread[] radnici;
        private readonly Random random = new Random();
        private readonly Dictionary<Guid, JobHandle> jobHandles = new Dictionary<Guid, JobHandle>();
        // Konstruktor koji ucitava konfiguraciju iz XML-a, inicijalizuje red poslova i pokrece worker niti
        public ProcessingSystem()
        {
            XElement sistem = XElement.Load("..\\..\\SystemConfig.xml");
            WorkerCount = (int)sistem.Element("WorkerCount");
            MaxQueueSize = (int)sistem.Element("MaxQueueSize");

            Jobs = new SortedSet<Job>(Comparer<Job>.Create((a, b) =>
                a.Priority != b.Priority ?
                a.Priority.CompareTo(b.Priority) :
                a.Id.CompareTo(b.Id)));

            // Ucitaj pocetne jobove iz XML-a
            foreach (var j in sistem.Element("Jobs").Elements("Job"))
            {
                Jobs.Add(new Job(
                    Helpers.ConvertStringToJobType((string)j.Attribute("Type")),
                    (string)j.Attribute("Payload"),
                    (int)j.Attribute("Priority")
                ));
            }

            // Pokreni worker niti
            radnici = new Thread[WorkerCount];
            for (int i = 0; i < WorkerCount; i++)
            {
                radnici[i] = new Thread(WorkerLoop);
                radnici[i].IsBackground = true;
                radnici[i].Start();
            }
        }
        // Funkcija koja ce se beskonacno izvrsavati u radnim nitima i cekati na poslove dok ne stignu
        private void WorkerLoop()
        {
            while (true)
            {
                Job job = null;

                lock (_lock)
                {
                    // Zaustavi se dok nema poslova u redu
                    while (Jobs.Count == 0)
                    { 
                        Monitor.Wait(_lock);
                    }
                    // Kada dodju poslovi uzmi onaj sa najvisim prioritetom odnosno sa najmanjim brojem i obradi ga
                    job = Jobs.Min;
                    Jobs.Remove(job);
                }
                // Kada se izvadi posao iz reda, obradi ga i uhvati eventualne greske
                ExecuteWithRetry(job);
            }
        }

        private void ExecuteWithRetry(Job job)
        {
            int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var task = Task.Run(() => ProcessJob(job));
                    bool completed = task.Wait(2000);

                    if (!completed)
                        throw new TimeoutException($"Job {job.Id} timed out.");

                    int result = task.Result;
                    JobCompleted?.Invoke(job, result);

                    lock (_lock)
                    {
                        if (jobHandles.TryGetValue(job.Id, out JobHandle handle))
                        {
                            handle.Complete(-1);
                            jobHandles.Remove(job.Id);
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Job {job.Id} failed attempt {attempt}: {ex.Message}");

                    if (attempt == maxAttempts)
                    {
                        // Final failure - fire JobFailed but log as ABORT
                        JobFailed?.Invoke(job, "ABORT");
                        lock (_lock) { jobHandles.Remove(job.Id); }
                    }
                }
            }
        }

        private int ProcessJob(Job job)
        {
            // Idempotency check
            lock (_lock)
            {
                // Proveri da li je posao vec u set-u izvrsenih poslova 
                if (executedJobIds.Contains(job.Id))
                {
                    Console.WriteLine($"Job {job.Id} already executed, skipping.");
                    return -1;
                }
            }
            int result;
            // Obradi posao dal PRIME ili IO
            if (job.Type == JOBTYPE.PRIME)
            {
                result = ProcessPrimeJob(job.Payload);
            }
            else if (job.Type == JOBTYPE.IO) {
                result= ProcessIOJob(job.Payload);
            }
            else
            {
                result = 0;
            }

            lock (_lock)
            {
                executedJobIds.Add(job.Id); // mark only after real execution
            }
            return result;
        }

        private int ProcessPrimeJob(string payload)
        {
            // Payload format: "numbers:10_000,threads:3"
            var parts = payload.Split(',');
            int limit = int.Parse(parts[0].Split(':')[1].Replace("_",""));
            int threadCount = int.Parse(parts[1].Split(':')[1].Replace("_", ""));

            // Ogranici broj niti na [1,8]
            threadCount = Math.Max(1, Math.Min(8, threadCount));

            // Paralelno izracunavanje prostih brojeva
            int primeCount = 0;
            object countLock = new object();

            // Podeliti posao izmedju niti
            int rangeSize = limit / threadCount;
            Thread[] threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int start = i * rangeSize + 2;
                int end = (i == threadCount - 1) ? limit : start + rangeSize;

                threads[i] = new Thread(() =>
                {
                    int localCount = 0;
                    for (int n = start; n <= end; n++)
                    {
                        if (IsPrime(n))
                            localCount++;
                    }
                    lock (countLock)
                    {
                        primeCount += localCount;
                    }
                });
                threads[i].Start();
            }

            // Sacekaj sve niti
            foreach (var t in threads)
                t.Join();

            Console.WriteLine($"Prime job done. Primes up to {limit}: {primeCount}");
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
            // Payload format: "delay:1_000"
            int delay = int.Parse(payload.Split(':')[1].Replace("_", ""));

            Console.WriteLine($"IO job starting, simulating {delay}ms delay...");
            Thread.Sleep(delay);

            int result = random.Next(0, 101);
            Console.WriteLine($"IO job done. Result: {result}");
            return result;
        }

        public JobHandle Submit(Job job)
        {
            JobHandle handle = new JobHandle();

            lock (_lock)
            {
                if (Jobs.Count >= MaxQueueSize)
                {
                    Console.WriteLine($"Queue full! Job {job.Id} rejected.");
                    return null;
                }
                jobHandles[job.Id] = handle;
                Jobs.Add(job);
                Monitor.Pulse(_lock); // probudi jednog radnika
            }

            return handle;
        }
    }
}