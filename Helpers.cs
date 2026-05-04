using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public enum JOBTYPE
    {
        Job1,
        Job2,
        Job3
    }
    public class Helpers
    {
        public static string ConvertJobTypeToString(JOBTYPE jobType)
        {
            switch (jobType)
            {
                case JOBTYPE.Job1:
                    return "Job1";
                case JOBTYPE.Job2:
                    return "Job2";
                case JOBTYPE.Job3:
                    return "Job3";
                default:
                    return "Unknown";
            }
        }
    }
}
