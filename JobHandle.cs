using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class JobHandle
    {
        // Polja klase 
        public Guid Id { get; set; }
        public Task<int> Result { get; set; }

        // Konstruktor klase
        public JobHandle(Task<int> result)
        {
            Id = Guid.NewGuid();
            Result = result;
        }

        //Ispis
        public override string ToString()
        {
            return "JobHandle ID: " + Id.ToString();
        }
    }
}
