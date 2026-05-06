using System;

namespace Kolokvijum1
{
    public class Job
    {
        // Polja 
        public Guid Id { get; set; }
        public JOBTYPE Type { get; set; }
        public string Payload { get; set; }
        public int Priority { get; set; }   

        // Vreme kada je posao predat sistemu 
        public DateTime SubmittedAt { get; set; }
        // Konstruktor
        public Job(JOBTYPE type = JOBTYPE.PRIME, string payload = "", int priority = 0)
        {
            Id = Guid.NewGuid();
            Type = type;
            Payload = payload;
            Priority = priority;
            SubmittedAt = DateTime.Now;
        }
        // Ispis
        public override string ToString()
        {
            return $"{Helpers.ConvertJobTypeToString(Type)} (Priority={Priority}, Payload={Payload})";
        }
    }
}
