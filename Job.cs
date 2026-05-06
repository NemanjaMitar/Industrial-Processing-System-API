using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class Job
    {
        // Polja klase Job
        public JOBTYPE Type { get; set; }
        public string Payload { get; set; }
        public Guid Id { get; set; }
        public int Priority { get; set; }

        // Konstruktor klase 
        public Job(JOBTYPE type = JOBTYPE.PRIME, string payload = "", int priority = 0)
        {
            Type = type;
            Payload = payload;
            Id = Guid.NewGuid();
            Priority = priority;
        }
        // Ispis
        public override string ToString()
        {
            return Helpers.ConvertJobTypeToString(Type) + ": " + Payload;
        }
    }
}
