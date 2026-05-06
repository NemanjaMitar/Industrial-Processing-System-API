using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kolokvijum1
{
    // EventSystem se pretplacuje na dogadjaje (JobCompleted, JobFailed)
    // Ispisuje u log.txt 
    // Asinhrono pisanje: koristimo Task.Run + lock, pa pozivajuca nit nije blokirana.
    public class EventSystem
    {
        private readonly string _logFile;
        private readonly object _fileLock = new object();

        public EventSystem(ProcessingSystem processingSystem, string logFile = "log.txt")
        {
            _logFile = logFile;

            // Pretplata na dogadjaje koristeci lamda izraze
            processingSystem.JobCompleted += (job, result) =>
            {
                _ = LogAsync(job.Id, "COMPLETED", result.ToString());
            };

            processingSystem.JobFailed += async (job, status) =>
            {
                await LogAsync(job.Id, status, "-1");
            };
        }
        // Upis u log fajl 
        private Task LogAsync(Guid jobId, string status, string result)
        {
            return Task.Run(() =>
            {
                lock (_fileLock)
                {
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{status}] {jobId}, {result}";
                    using (StreamWriter sw = new StreamWriter(_logFile, append: true))
                        sw.WriteLine(line);
                    Console.WriteLine($"[LOG] [{status}] {jobId} -> {result}");
                }
            });
        }
    }
}
