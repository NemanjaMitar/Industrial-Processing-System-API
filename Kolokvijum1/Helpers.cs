using System;


// Samo helperi za jobtype i pri parsiranju xmla
namespace Kolokvijum1
{
    public enum JOBTYPE
    {
        PRIME,
        IO,
    }

    // Pomocne metode za konverziju enum-a u string i obrnuto
    public static class Helpers
    {
        public static string ConvertJobTypeToString(JOBTYPE jobType)
        {
            switch (jobType)
            {
                case JOBTYPE.PRIME: return "PRIME";
                case JOBTYPE.IO: return "IO";
                default: throw new ArgumentException("Invalid job type");
            }
        }

        public static JOBTYPE ConvertStringToJobType(string jobType)
        {
            switch (jobType)
            {
                case "Prime":
                case "PRIME":
                    return JOBTYPE.PRIME;
                case "IO":
                case "Io":
                    return JOBTYPE.IO;
                default:
                    throw new ArgumentException($"Invalid job type string: {jobType}");
            }
        }
    }
}
