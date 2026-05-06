using System;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    // Predstavlja rezultat izvrsavanja posla.
    // Pozivalac dobija JobHandle iz Submit() i moze da await-uje Result.
    public class JobHandle
    {
        private readonly TaskCompletionSource<int> _tcs;

        public Guid Id { get; }
        public Task<int> Result => _tcs.Task;

        public JobHandle(Guid jobId)
        {
            Id = jobId;
            _tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // Worker thread poziva ovo kada posao uspesno zavrsi
        public bool Complete(int result) => _tcs.TrySetResult(result);

        // Worker thread poziva ovo kada se posao ABORT-uje (3 puta failovao)
        public bool Fail(Exception ex) => _tcs.TrySetException(ex);

        public override string ToString() => $"JobHandle(Id={Id})";
    }

    // Custom exception koji se baca kada posao ABORT-uje nakon 3 pokusaja
    public class JobAbortedException : Exception
    {
        public Guid JobId { get; }
        public JobAbortedException(Guid jobId, string message) : base(message)
        {
            JobId = jobId;
        }
    }
}
