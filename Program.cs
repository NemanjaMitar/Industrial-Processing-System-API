using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
            //Job job1 = new Job(JOBTYPE.PRIME, "Software Developer");
            //Job job2 = new Job(JOBTYPE.IO, "Graphic Designer");

            //Console.WriteLine(job1);
            //Console.WriteLine(job2);
            //Task<int> task1 = Task.Run(() => 42);
            //JobHandle jobHandler = new JobHandle(task1);

            //Console.WriteLine(jobHandler);

            //ProcessingSystem processingSystem = new ProcessingSystem();
            //Console.WriteLine($"Max Queue Size: {processingSystem.MaxQueueSize}, Worker Count: {processingSystem.WorkerCount}");

            //foreach (var job in processingSystem.Jobs) 
            //{
            //    Console.WriteLine(job);
            //}
            try
            {
                // Inicijalizuj sistem i event sistem
                ProcessingSystem system = new ProcessingSystem();
                EventSystem eventSystem = new EventSystem(system);

                Console.WriteLine($"System started - Workers: {system.WorkerCount}, Max Queue: {system.MaxQueueSize}");
                Console.WriteLine($"Loaded {system.Jobs.Count} jobs from XML");
                Console.WriteLine("--------------------------------------------------");

                // Prikazi ucitane jobove iz XML-a
                foreach (var job in system.Jobs)
                    Console.WriteLine($"Loaded job: {job}  Priority: {job.Priority}");

                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("Submitting additional jobs manually...");

                // Rucno dodaj nekoliko jobova
                JobHandle h1 = system.Submit(new Job(JOBTYPE.PRIME, "numbers:5_000,threads:2", priority: 1));
                JobHandle h2 = system.Submit(new Job(JOBTYPE.IO, "delay:500", priority: 2));
                JobHandle h3 = system.Submit(new Job(JOBTYPE.IO, "delay:100", priority: 1));

                Console.WriteLine($"Submitted 3 jobs manually.");
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("Processing jobs... check log.txt for events.");
                Console.WriteLine("Press Enter to exit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }


            Console.ReadLine();
        }
    }
}
