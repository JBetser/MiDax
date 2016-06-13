using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public abstract class TimeDecay
    {
        protected TimeSeries _timeDecayWeight = new TimeSeriesConstant(1m);
        protected decimal _curDecayWeight = 1m;
        protected decimal _cumDecayWeight = 1m;
        public TimeSeries TimeDecaySeries { get { return _timeDecayWeight; } }
        public decimal CurDecayWeight { get { return _curDecayWeight; } }
        public decimal CumDecayWeight { get { return _cumDecayWeight; } }

        public abstract decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime);
        public abstract decimal Update(DateTime beginPeriod, DateTime endPeriod, DateTime curTime);
        public abstract void UpdateDecrement(DateTime startTime, DateTime curTime, decimal decrement);
    }

    public class TimeDecayNull : TimeDecay
    {
        TimeSpan _mobilePeriodLength; 
  
        public TimeDecayNull(TimeSpan mobilePeriodLength)
        {
            _mobilePeriodLength = mobilePeriodLength;
        }

        public override decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime)
        {
            return (decimal)(endPeriod > beginPeriod ? endPeriod - beginPeriod : endPeriod - beginPeriod.AddMilliseconds(-(double)totalPeriodMilliSec)).TotalMilliseconds / totalPeriodMilliSec;
        }
        public override decimal Update(DateTime beginPeriod, DateTime endPeriod, DateTime curTime)
        {
            var periodMs = (decimal)_mobilePeriodLength.TotalMilliseconds;
            var nextTimeDecayWeight = beginPeriod == endPeriod ? 0m : Weight(beginPeriod, endPeriod, periodMs, curTime);
            if (endPeriod > _timeDecayWeight.EndTime())
                _timeDecayWeight.Add(endPeriod, new Price(nextTimeDecayWeight));
            return nextTimeDecayWeight;
        }
        public override void UpdateDecrement(DateTime startTime, DateTime curTime, decimal decrement)
        {
        }
    }

    public sealed class TimeDecayLinear : TimeDecayNull
    {
        decimal _startCoeff;
        decimal _endCoeff;             

        public TimeDecayLinear(decimal linearDecay, TimeSpan mobilePeriodLength) : base(mobilePeriodLength)
        {
            _startCoeff = 2m / (1m + linearDecay);
            _endCoeff = 2m - _startCoeff;            
            _timeDecayWeight = new TimeSeries();
        }
        public override decimal Weight(DateTime beginPeriod, DateTime endPeriod, decimal totalPeriodMilliSec, DateTime? startTime)
        {
            var periodWeight = base.Weight(beginPeriod, endPeriod, totalPeriodMilliSec, null);
            var decayAvgPeriod = new DateTime((beginPeriod.Ticks + endPeriod.Ticks) / 2);
            var decayWeight = _startCoeff + (_endCoeff - _startCoeff) * base.Weight(startTime.Value, decayAvgPeriod, totalPeriodMilliSec, null);
            return periodWeight * decayWeight;
        }
        public override void UpdateDecrement(DateTime startTime, DateTime curTime, decimal decrementWeight) 
        {
            var nextTimeDecayWeight = _timeDecayWeight[curTime];
            if (nextTimeDecayWeight == null)
                return;
            /*var beginTimeDecayWeight = _timeDecayWeight[startTime];
            if (beginTimeDecayWeight == null)
                return;*/
            /*
            var endDecTime = curTime - _mobilePeriodLength;
            var decLen = endDecTime - startTime;
            var decValues = _timeDecayWeight.Values(curTime - _mobilePeriodLength, decLen);
            if (decValues == null)
                return;
            var avgDecWeight = 0m;
            var beginPeriod = startTime;
            var lastWeight = 0m;
            foreach (var val in decValues)
            {
                avgDecWeight += val.Value.Bid * (decimal)(val.Key - beginPeriod).TotalMilliseconds;
                lastWeight = val.Value.Bid;
                beginPeriod = val.Key;
            }
            avgDecWeight += lastWeight * (decimal)(endDecTime - beginPeriod).TotalMilliseconds;
            avgDecWeight = decrement / (decimal)decLen.TotalMilliseconds;      */      
            //var newAvg = (curAvg - decrementAvg * beginTimeDecayWeight.Value.Value.Bid * _cumDecayWeight * prevVolumeWeight);
            _curDecayWeight = (1m - nextTimeDecayWeight.Value.Value.Bid) / (1m - decrementWeight * _cumDecayWeight);
            /*newAvg *= nextDecayWeight;
            newAvg += incrementAvg * curVolumeWeight;*/
            _cumDecayWeight *= _curDecayWeight;
            //return newAvg;
        }
    }
}
