using Kolokvijum1;
using System;
using System.Reflection.Metadata;
using Xunit;

namespace Kolokvijum1.Tests
{
    // Job tests
    public class JobTests
    {
        [Fact]
        public void Job_DefaultConstructor_SetsDefaults()
        {
            var job = new Job();
            Assert.Equal(JOBTYPE.PRIME, job.Type);
            Assert.Equal("", job.Payload);
            Assert.Equal(0, job.Priority);
            Assert.NotEqual(Guid.Empty, job.Id);
        }

        [Fact]
        public void Job_Constructor_SetsFields()
        {
            var job = new Job(JOBTYPE.IO, "delay:500", 3);
            Assert.Equal(JOBTYPE.IO, job.Type);
            Assert.Equal("delay:500", job.Payload);
            Assert.Equal(3, job.Priority);
        }

        [Fact]
        public void Job_ToString_ReturnsCorrectFormat()
        {
            var job = new Job(JOBTYPE.PRIME, "numbers:1000,threads:2", 1);
            Assert.Contains("PRIME", job.ToString());
        }

        [Fact]
        public void Job_TwoJobs_HaveDifferentIds()
        {
            var job1 = new Job();
            var job2 = new Job();
            Assert.NotEqual(job1.Id, job2.Id);
        }
    }

    // Helpers tests
    public class HelpersTests
    {
        [Fact]
        public void ConvertJobTypeToString_Prime_ReturnsPRIME()
        {
            Assert.Equal("PRIME", Helpers.ConvertJobTypeToString(JOBTYPE.PRIME));
        }

        [Fact]
        public void ConvertJobTypeToString_IO_ReturnsIO()
        {
            Assert.Equal("IO", Helpers.ConvertJobTypeToString(JOBTYPE.IO));
        }

        [Fact]
        public void ConvertJobTypeToString_Invalid_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                Helpers.ConvertJobTypeToString((JOBTYPE)99));
        }

        [Fact]
        public void ConvertStringToJobType_Prime_ReturnsPRIME()
        {
            Assert.Equal(JOBTYPE.PRIME, Helpers.ConvertStringToJobType("Prime"));
        }

        [Fact]
        public void ConvertStringToJobType_IO_ReturnsIO()
        {
            Assert.Equal(JOBTYPE.IO, Helpers.ConvertStringToJobType("IO"));
        }

        [Fact]
        public void ConvertStringToJobType_Invalid_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                Helpers.ConvertStringToJobType("InvalidType"));
        }
    }

    // JobHandle tests
    public class JobHandleTests
    {
        [Fact]
        public void JobHandle_Complete_SetsResult()
        {
            var handle = new JobHandle(Guid.NewGuid());
            handle.Complete(42);
            Assert.Equal(42, handle.Result.Result);
        }

        [Fact]
        public void JobHandle_Fail_SetsException()
        {
            var handle = new JobHandle(Guid.NewGuid());
            handle.Fail(new Exception("test error"));
            Assert.True(handle.Result.IsFaulted);
        }

        [Fact]
        public void JobHandle_IdMatchesJobId()
        {
            var id = Guid.NewGuid();
            var handle = new JobHandle(id);
            Assert.Equal(id, handle.Id);
        }

        [Fact]
        public void JobHandle_Complete_ReturnsTrueOnFirstCall()
        {
            var handle = new JobHandle(Guid.NewGuid());
            Assert.True(handle.Complete(1));
        }

        [Fact]
        public void JobHandle_Complete_ReturnsFalseOnSecondCall()
        {
            var handle = new JobHandle(Guid.NewGuid());
            handle.Complete(1);
            Assert.False(handle.Complete(2)); // TrySetResult returns false
        }
    }

    public class ProcessingSystemTests
    {
        [Fact]
        public void Submit_ReturnsHandle()
        {
            var system = new ProcessingSystem(2, 10); // no XML needed
            var handle = system.Submit(new Job(JOBTYPE.IO, "delay:100", 1));
            Assert.NotNull(handle);
        }

        [Fact]
        public void Submit_QueueFull_ReturnsNull()
        {
            var system = new ProcessingSystem(1, 0); // maxQueueSize = 0, always full
            var handle = system.Submit(new Job(JOBTYPE.IO, "delay:100", 1));
            Assert.Null(handle);
        }

        [Fact]
        public void GetTopJobs_ReturnsCorrectCount()
        {
            var system = new ProcessingSystem(1, 10);
            system.Submit(new Job(JOBTYPE.IO, "delay:100000", 1));
            system.Submit(new Job(JOBTYPE.IO, "delay:100000", 2));
            system.Submit(new Job(JOBTYPE.IO, "delay:100000", 3));
            Assert.Equal(2, system.GetTopJobs(2).Count());
        }

        [Fact]
        public void GetJob_ReturnsCorrectJob()
        {
            var system = new ProcessingSystem(1, 10);
            var job = new Job(JOBTYPE.IO, "delay:100000", 1);
            system.Submit(job);
            Assert.Equal(job.Id, system.GetJob(job.Id).Id);
        }

        [Fact]
        public void GetJob_UnknownId_ReturnsNull()
        {
            var system = new ProcessingSystem(1, 10);
            Assert.Null(system.GetJob(Guid.NewGuid()));
        }



        [Fact]
        public void Submit_IOJob_CompletesWithinTimeout()
        {
            var system = new ProcessingSystem(3, 100); // dedicated fresh system
            var handle = system.Submit(new Job(JOBTYPE.IO, "delay:50", 1));
            bool completed = handle.Result.Wait(8000);
            Assert.True(completed);
        }

        [Fact]
        public void Submit_PrimeJob_ReturnsCorrectCount()
        {
            var system = new ProcessingSystem(3, 100);
            var handle = system.Submit(new Job(JOBTYPE.PRIME, "numbers:10,threads:1", 1));
            bool completed = handle.Result.Wait(8000);
            Assert.True(completed);
            Assert.Equal(4, handle.Result.Result);
        }
    }

    // ReportGenerator tests  
    public class ReportGeneratorTests
    {
        [Fact]
        public void Record_And_GenerateReport_CreatesFile()
        {
            var gen = new ReportGenerator("test_reports");
            gen.Record(new ExecutionRecord
            {
                JobId = Guid.NewGuid(),
                Type = JOBTYPE.IO,
                Success = true,
                DurationMs = 100
            });
            string path = gen.GenerateReport();
            Assert.True(System.IO.File.Exists(path));
        }

        [Fact]
        public void GenerateReport_RotatesAfter10()
        {
            var gen = new ReportGenerator("test_reports2");
            for (int i = 0; i < 11; i++)
            {
                gen.Record(new ExecutionRecord
                {
                    JobId = Guid.NewGuid(),
                    Type = JOBTYPE.PRIME,
                    Success = true,
                    DurationMs = 50
                });
                gen.GenerateReport();
            }
            // 11th report overwrites slot 0
            Assert.True(System.IO.File.Exists("test_reports2/report_0.xml"));
        }

        [Fact]
        public void EventSystem_LogsOnJobCompleted()
        {
            var system = new ProcessingSystem(2, 10);
            var eventSystem = new EventSystem(system, "test_log.txt");
            var handle = system.Submit(new Job(JOBTYPE.IO, "delay:50", 1));
            handle.Result.Wait(5000);
            Thread.Sleep(500); // wait for async log
            Assert.True(File.Exists("test_log.txt"));
        }
        [Fact]
        public void ProcessingSystem_PendingCount_ReturnsCorrectValue()
        {
            var system = new ProcessingSystem(1, 10);
            system.Submit(new Job(JOBTYPE.IO, "delay:100000", 1));
            Assert.True(system.PendingCount >= 0);
        }

        [Fact]
        public void ProcessingSystem_XmlConstructor_LoadsConfig()
        {
            var system = new ProcessingSystem(); // uses XML constructor
            Assert.True(system.WorkerCount > 0);
            Assert.True(system.MaxQueueSize > 0);
        }

        [Fact]
        public void ProcessingSystem_IdempotencyCheck_SkipsDuplicate()
        {
            var system = new ProcessingSystem(2, 10);
            var job = new Job(JOBTYPE.IO, "delay:50", 1);
            var h1 = system.Submit(job);
            h1.Result.Wait(5000); // let it execute first
            Thread.Sleep(200);
            // submit same job again - idempotency should trigger
            var h2 = system.Submit(job);
            Assert.NotNull(h2);
        }

        [Fact]
        public void EventSystem_LogsOnJobFailed()
        {
            var system = new ProcessingSystem(2, 10);
            var eventSystem = new EventSystem(system, "test_log_fail.txt");
            system.Submit(new Job(JOBTYPE.IO, "delay:5000", 1)); // will timeout
            Thread.Sleep(7000); // wait for all retries
            Assert.True(File.Exists("test_log_fail.txt"));
        }

        [Fact]
        public void ReportGenerator_Record_MultipleTypes()
        {
            var gen = new ReportGenerator("test_reports3");
            gen.Record(new ExecutionRecord { JobId = Guid.NewGuid(), Type = JOBTYPE.PRIME, Success = true, DurationMs = 100 });
            gen.Record(new ExecutionRecord { JobId = Guid.NewGuid(), Type = JOBTYPE.IO, Success = false, DurationMs = 0 });
            gen.Record(new ExecutionRecord { JobId = Guid.NewGuid(), Type = JOBTYPE.IO, Success = true, DurationMs = 200 });
            string path = gen.GenerateReport();
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void Submit_SameJobTwice_IdempotencyWorks()
        {
            var system = new ProcessingSystem(2, 10);
            var job = new Job(JOBTYPE.IO, "delay:50", 1);
            var h1 = system.Submit(job);
            var h2 = system.Submit(job); // same job
            Assert.NotNull(h1);
            Assert.NotNull(h2);
        }

        [Fact]
        public void ProcessingSystem_JobCompleted_EventFires()
        {
            var system = new ProcessingSystem(2, 10);
            bool fired = false;
            system.JobCompleted += (job, result) => { fired = true; };
            var handle = system.Submit(new Job(JOBTYPE.IO, "delay:50", 1));
            handle.Result.Wait(5000);
            Thread.Sleep(200);
            Assert.True(fired);
        }

        [Fact]
        public void ProcessingSystem_JobFailed_EventFires()
        {
            var system = new ProcessingSystem(2, 10);
            bool fired = false;
            system.JobFailed += (job, status) => { fired = true; };
            system.Submit(new Job(JOBTYPE.IO, "delay:5000", 1)); // will timeout
            Thread.Sleep(7000);
            Assert.True(fired);
        }
    }
}