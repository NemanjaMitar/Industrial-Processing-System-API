using System;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    // Predstavlja rezultat izvrsavanja posla.
    public class JobHandle
    {
        // Vraca povratnu vrednost posla kada se posao zavrsi (COMPLETED) ili baci exception na abort
        private readonly TaskCompletionSource<int> _tcs;
        // Polja
        public Guid Id { get; }
        public Task<int> Result => _tcs.Task;

        public JobHandle(Guid jobId)
        {
            Id = jobId;
            _tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public bool Complete(int result) => _tcs.TrySetResult(result);

        public bool Fail(Exception ex) => _tcs.TrySetException(ex);

        public override string ToString() => $"JobHandle(Id={Id})";
    }

    //  exception koji se baca kada posao ABORT-uje nakon 3 pokusaja
    public class JobAbortedException : Exception
    {
        public Guid JobId { get; }
        public JobAbortedException(Guid jobId, string message) : base(message)
        {
            JobId = jobId;
        }
    }
}
