using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class EventSystem
    {
        private readonly string _logFile = "log.txt";
        private readonly object _fileLock = new object();

        public EventSystem(ProcessingSystem processingSystem)
        {
            processingSystem.JobCompleted += async (job, result) =>
            {
                await LogAsync(job.Id, "COMPLETED", result.ToString());
            };

            processingSystem.JobFailed += async (job, status) =>
            {
                await LogAsync(job.Id, status, "-1");
            };
        }

        private async Task LogAsync(Guid jobId, string status, string result)
        {
            string line = $"[{DateTime.Now}] [{status}] {jobId}, {result}";
            await Task.Run(() =>
            {
                lock (_fileLock)
                {
                    using (StreamWriter sw = new StreamWriter(_logFile, append: true))
                    {
                        sw.WriteLine(line);
                    }
                }
            });
        }

    }
}
