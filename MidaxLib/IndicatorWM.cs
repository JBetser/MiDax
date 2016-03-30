using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class IndicatorWMA : Indicator
    {
        protected int _periodSeconds;
        protected decimal _periodMilliSeconds;
        protected TimeDecay _timeDecay;
        protected TimeDecay _timeDecayNull = new TimeDecayNull();
        protected Price _curavg = null;
        protected DateTime _curavgTime;
        protected DateTime _decrementStartTime;
        protected Price _incrementAvg = null;
        decimal _decayRatio = 0m;

        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }
        public TimeDecay TimeDecay { get { return _timeDecay; } }

        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            if (Config.Settings.ContainsKey("TIME_DECAY_FACTOR"))
                _timeDecay = new TimeDecayLinear(int.Parse(Config.Settings["TIME_DECAY_FACTOR"]));
            else
                _timeDecay = _timeDecayNull;
        }

        public IndicatorWMA(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = new TimeDecayLinear(3m);
        }

        public IndicatorWMA(IndicatorWMA indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = indicator.TimeDecay;
        }

        public Price Average(DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            var avg = new Price();
            Average(ref avg, startTime, updateTime, acceptMissingValues);
            return avg;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.TotalMinutes(updateTime) >= (double)_periodSeconds / 60.0)
            {
                Price avgPrice = IndicatorFunc(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);                    
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }
        
        Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            DateTime startTime;
            if (_curavg == null){
                startTime = updateTime.AddSeconds(-_periodSeconds);
                _decrementStartTime = startTime;
                _curavg = new Price();
            }
            else{
                startTime = _curavgTime;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
                var decrementAvg = new Price();
                MobileAverage(ref decrementAvg, _decrementStartTime, decrementUpdateTime);
                _curavg = _timeDecay.Decrement(_curavg, decrementAvg, (decimal)(decrementUpdateTime - _decrementStartTime).TotalMilliseconds / (decimal)(_periodSeconds * 1000));
                _decrementStartTime = decrementUpdateTime;
            }
            _incrementAvg = new Price();
            MobileAverage(ref _incrementAvg, startTime, updateTime, false, updateTime.AddSeconds(-_periodSeconds));
            _curavg += _incrementAvg;
            _curavgTime = updateTime;
            return _curavg;
        }

        protected virtual void MobileAverage(ref Price curavg, DateTime startTime, DateTime updateTime, bool acceptMissingValues = false, DateTime? origin = null, TimeDecay decay = null)
        {
            Average(ref curavg, startTime, updateTime, acceptMissingValues, origin, decay);
        }        

        protected bool Average(ref Price curavg, DateTime startTime, DateTime updateTime, bool acceptMissingValues = false, DateTime? origin = null, TimeDecay decay = null)
        {
            bool started = false;
            //bool linear_adjusted = false;
            TimeDecay timeDecay = decay == null ? _timeDecay : decay;
            DateTime originTime = origin.HasValue ? origin.Value : startTime;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {                
                if (!started)
                {
                    started = true;
                    var diffTime = (int)(startTime - endPeriodValue.Key).TotalMilliseconds;
                    if (!acceptMissingValues && diffTime < 0)
                        return false;
                    startTimePrev = endPeriodValue.Key;
                    //linear_adjusted = diffTime == 0;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                /*
                else if (!linear_adjusted)
                {
                    linear_adjusted = true;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, beginPeriodValue.Value + (decimal)((startTime - startTimePrev).TotalMilliseconds / (endPeriodValue.Key - startTimePrev).TotalMilliseconds)
                                                                        * (endPeriodValue.Value.Bid - beginPeriodValue.Value.Bid));
                }*/
                //if (linearInterpolation)
                //curavg += ((beginPeriodValue.Value + endPeriodValue.Value) / 2m) * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, _periodMilliSeconds, originTime);
                //else
                curavg += beginPeriodValue.Value * timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, _periodMilliSeconds, originTime);
                beginPeriodValue = endPeriodValue;
            }
            curavg += beginPeriodValue.Value * timeDecay.Weight(beginPeriodValue.Key, updateTime, _periodMilliSeconds, originTime);
            return true;
        }        
    }

    public class IndicatorWMVol : IndicatorWMA
    {
        public IndicatorWMVol(MarketData mktData, int periodMinutes)
            : base("WMVol_" + periodMinutes + "_" + mktData.Id, mktData, periodMinutes)
        {
        }

        protected override void MobileAverage(ref Price curVolAvg, DateTime startTime, DateTime updateTime, bool acceptMissingValues = false, DateTime? origin = null, TimeDecay decay = null)
        {
            Price var = new Price();
            bool started = false;
            TimeDecay timeDecay = decay == null ? _timeDecay : decay;
            DateTime originTime = origin.HasValue ? origin.Value : startTime;

            DateTime avgStartTime = updateTime.AddSeconds(-_periodSeconds);
            var avg = new Price();
            if (!Average(ref avg, avgStartTime, updateTime, acceptMissingValues))
                return;

            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    if (!acceptMissingValues && Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalMilliseconds) != 0)
                        return;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                curVolAvg += (beginPeriodValue.Value - avg).Abs() * timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, _periodMilliSeconds, originTime);
                beginPeriodValue = endPeriodValue;
            }
            if (beginPeriodValue.Value == null)
                return;
            curVolAvg += (beginPeriodValue.Value - avg).Abs() * timeDecay.Weight(beginPeriodValue.Key, updateTime, _periodMilliSeconds, originTime);
        }
    }
}
