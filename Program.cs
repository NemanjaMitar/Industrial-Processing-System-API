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
            Job job1 = new Job(JOBTYPE.Job1, "Software Developer");
            Job job2 = new Job(JOBTYPE.Job2, "Graphic Designer");
            Job job3 = new Job(JOBTYPE.Job3, "Data Analyst");

            Console.WriteLine(job1);
            Console.WriteLine(job2);
            Console.WriteLine(job3);
            Task<int> task1 = Task.Run(() => 42);
            JobHandle jobHandler = new JobHandle(task1);

            Console.WriteLine(jobHandler);
            Console.ReadLine();
        }
    }
}
