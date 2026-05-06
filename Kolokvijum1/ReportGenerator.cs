using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Kolokvijum1
{
    // Jedan zapis o izvrsenom (ili neuspelom) poslu.
    // Sluzi za generisanje izvestaja koriscenjem LINQ-a.
    public class ExecutionRecord
    {
        public Guid JobId { get; set; }
        public JOBTYPE Type { get; set; }
        public bool Success { get; set; }      
        public double DurationMs { get; set; } 
        public DateTime FinishedAt { get; set; }
    }

    // Generise XML izvestaj na svaki minut.
    // Cuva poslednjih 10 izvestaja u fajlovima report_0.xml ... report_9.xml,
    // pa 11. izvestaj prepisuje najstariji (mod 10).
    public class ReportGenerator : IDisposable
    {
        private readonly List<ExecutionRecord> _records = new List<ExecutionRecord>();
        private readonly object _recordsLock = new object();
        private readonly Timer _timer;
        private readonly string _folder;
        private int _counter = 0;

        public ReportGenerator(string folder = "reports", TimeSpan? interval = null)
        {
            _folder = folder;
            Directory.CreateDirectory(_folder);

            // Default: svaki minut
            var period = interval ?? TimeSpan.FromMinutes(1);
            _timer = new Timer(_ => GenerateReport(), null, period, period);
        }

        // Worker thread prijavljuje izvrsene poslove
        public void Record(ExecutionRecord record)
        {
            lock (_recordsLock)
            {
                _records.Add(record);
            }
        }

        // Eksplicitno generisanje izvestaja (npr. na kraju Main-a)
        public string GenerateReport()
        {
            List<ExecutionRecord> snapshot;
            lock (_recordsLock)
            {
                snapshot = _records.ToList();
            }

            // ====== LINQ agregacije (zahtev iz teksta zadatka) ======

            // 1) broj izvrsenih poslova po tipu
            var completedByType = snapshot
                .Where(r => r.Success)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderBy(x => x.Type)
                .ToList();

            // 2) prosecno vreme izvrsavanja po tipu (samo uspesni)
            var avgDurationByType = snapshot
                .Where(r => r.Success)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, AvgMs = g.Average(r => r.DurationMs) })
                .OrderBy(x => x.Type)
                .ToList();

            // 3) broj neuspesnih poslova po tipu (sortirano po tipu)
            var failedByType = snapshot
                .Where(r => !r.Success)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderBy(x => x.Type)
                .ToList();

            var doc = new XDocument(
                new XElement("Report",
                    new XAttribute("generatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    new XAttribute("totalRecords", snapshot.Count),

                    new XElement("CompletedByType",
                        completedByType.Select(x =>
                            new XElement("Item",
                                new XAttribute("type", x.Type),
                                new XAttribute("count", x.Count)))),

                    new XElement("AverageDurationByType",
                        avgDurationByType.Select(x =>
                            new XElement("Item",
                                new XAttribute("type", x.Type),
                                new XAttribute("avgMs", x.AvgMs.ToString("F2"))))),

                    new XElement("FailedByType",
                        failedByType.Select(x =>
                            new XElement("Item",
                                new XAttribute("type", x.Type),
                                new XAttribute("count", x.Count))))
                )
            );

            // Rotacija: poslednjih 10 izvestaja, 11. prepisuje najstariji
            int slot = _counter % 10;
            string path = Path.Combine(_folder, $"report_{slot}.xml");
            try
            {
                doc.Save(path);
                Console.WriteLine($"[REPORT] Generated {path} (total records: {snapshot.Count})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPORT] Failed to save: {ex.Message}");
            }
            Interlocked.Increment(ref _counter);
            return path;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
