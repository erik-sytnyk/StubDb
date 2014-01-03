using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StabDbTests
{
    public class BaseTest
    {
        public TimeSpan MeasureOperationTime(Action action)
        {
            var startTime = DateTime.Now;

            action.Invoke();

            var endTime = DateTime.Now;

            return endTime - startTime;
        }
    }
}
