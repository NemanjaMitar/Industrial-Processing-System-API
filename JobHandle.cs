using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class JobHandle
    {
        private readonly TaskCompletionSource<int> _tcs;
        // Polja klase 
        public Guid Id { get; set; }
        public Task<int> Result => _tcs.Task;

        // Konstruktor klase
        public JobHandle()
        {
            Id = Guid.NewGuid();
            _tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public bool Complete(int result )
        {
            return _tcs.TrySetResult(result);
        }


        //Ispis
        public override string ToString()
        {
            return "JobHandle ID: " + Id.ToString();
        }
    }
}
