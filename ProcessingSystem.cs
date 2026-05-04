using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kolokvijum1
{
    public class ProcessingSystem
    {
        public JobHandle Submit(Job job)
        {
            var task = Task.Run(() =>
            {
                // async processing logic will go here
                return 0;
            });
            return new JobHandle(task);
        }
    }
}
