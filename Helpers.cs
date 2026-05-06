using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public enum JOBTYPE
    {
        PRIME,
        IO,
    }
    public class Helpers
    {
        public static string ConvertJobTypeToString(JOBTYPE jobType)
        {
            switch (jobType)
            {
                case JOBTYPE.PRIME:
                    return "PRIME";
                case JOBTYPE.IO:
                    return "IO";
                default:
                   throw new Exception("Invalid job type");
            }
        }
        public static JOBTYPE ConvertStringToJobType(string jobType)
        {
            switch (jobType)
            {
                case "Prime":
                    return JOBTYPE.PRIME;
                case "IO":
                    return JOBTYPE.IO;
                default:
                    throw new Exception("Invalid job type string");
            }
        }
    }
}
