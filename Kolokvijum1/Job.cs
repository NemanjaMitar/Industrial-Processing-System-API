using System;

namespace Kolokvijum1
{
    public class Job
    {
        // Polja klase Job — prema specifikaciji
        public Guid Id { get; set; }
        public JOBTYPE Type { get; set; }
        public string Payload { get; set; }
        public int Priority { get; set; }   // manji broj => visi prioritet

        // Vreme kada je posao predat sistemu (za potrebe izvestaja)
        public DateTime SubmittedAt { get; set; }

        public Job(JOBTYPE type = JOBTYPE.PRIME, string payload = "", int priority = 0)
        {
            Id = Guid.NewGuid();
            Type = type;
            Payload = payload;
            Priority = priority;
            SubmittedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Helpers.ConvertJobTypeToString(Type)} (Priority={Priority}, Payload={Payload})";
        }
    }
}
