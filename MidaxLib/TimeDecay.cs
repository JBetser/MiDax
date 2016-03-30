using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public abstract class TimeDecay
    {
        public abstract decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime);
        public abstract Price Decrement(Price curAvg, Price decrementAvg, decimal timeWeight);
    }

    public class TimeDecayNull : TimeDecay
    {
        public override decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime)
        {
            return (decimal)(endPeriod - beginPeriod).TotalMilliseconds / totalPeriodMilliSec;
        }
        public override Price Decrement(Price curAvg, Price decrementAvg, decimal timeWeight)
        {
            return curAvg - decrementAvg;
        }
    }

    public sealed class TimeDecayLinear : TimeDecayNull
    {
        decimal _startCoeff;
        decimal _endCoeff;
        public TimeDecayLinear(decimal linearDecay)
        {
            _startCoeff = 2m / (1m + linearDecay);
            _endCoeff = 2m - _startCoeff;
        }
        public override decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime)
        {
            var periodWeight = base.Weight(beginPeriod, endPeriod, totalPeriodMilliSec, null);
            var decayWeight = (base.Weight(startTime.Value, beginPeriod, totalPeriodMilliSec, null) + base.Weight(startTime.Value, endPeriod, totalPeriodMilliSec, null)) / 2m;
            return periodWeight * (_startCoeff + (_endCoeff - _startCoeff) * decayWeight);
        }
        public override Price Decrement(Price curAvg, Price decrementAvg, decimal timeWeight)
        {
            timeWeight *= (_endCoeff - _startCoeff);
            var newAvg = curAvg - decrementAvg;
            newAvg *= (_startCoeff + _endCoeff - timeWeight) / (_startCoeff + timeWeight + _endCoeff);
            return newAvg;
        }
    }
}
