using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public abstract class TimeDecay
    {
        public abstract decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodSec);
    }

    public sealed class TimeDecayConstant : TimeDecay
    {
        public override decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodSec)
        {
            return (decimal)(endPeriod - beginPeriod).TotalMilliseconds / (1000.0m * totalPeriodSec);
        }
    }
}
