using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    /// <summary>
    /// Weighted Moving Average
    /// it gives more weight to recent data and as a result is more volatile than SMA
    /// 
    /// WMA = sum(weighted averages) / sum(weight)
    /// </summary>
    public class IndicatorWMA : Indicator
    {
        protected int _periodSeconds;
        protected TimeDecay _timeDecay;

        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }
        public TimeDecay TimeDecay { get { return _timeDecay; } }

        public IndicatorWMA(MarketData mktData, int periodMinutes, TimeDecay timeDecay = null)
            : base("WMA_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            if (timeDecay != null)
                _timeDecay = timeDecay;
            else
                _timeDecay = new TimeDecayConstant();
        }

        public IndicatorWMA(string id, MarketData mktData, int periodMinutes, TimeDecay timeDecay = null)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            if (timeDecay != null)
                _timeDecay = timeDecay;
            else
                _timeDecay = new TimeDecayConstant();
        }

        public IndicatorWMA(IndicatorWMA indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _timeDecay = indicator.TimeDecay;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.TotalMinutes(updateTime) > (double)_periodSeconds / 60.0)
            {
                Price avgPrice = IndicatorFunc(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }

        protected virtual Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            return Average(updateTime);
        }

        public Price Average(DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            Price avg = new Price();
            bool started = false;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    if (!acceptMissingValues && Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalMilliseconds) != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                if (linearInterpolation)
                    avg += ((beginPeriodValue.Value + endPeriodValue.Value) / 2m) * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, (decimal)_periodSeconds);
                else
                    avg += beginPeriodValue.Value * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, (decimal)_periodSeconds);
                beginPeriodValue = endPeriodValue;
            }
            avg += beginPeriodValue.Value * _timeDecay.Weight(beginPeriodValue.Key, updateTime, (decimal)_periodSeconds);
            return avg;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class IndicatorWMVol : IndicatorWMA
    {
        public IndicatorWMVol(MarketData mktData, int periodMinutes)
            : base("WMVol_" + periodMinutes + "_" + mktData.Id, mktData, periodMinutes)
        {
            _periodSeconds = periodMinutes * 60;
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            return AlgebricStdDev(mktData, updateTime);
        }

        public Price AlgebricStdDev(MarketData mktData, DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            Price var = new Price();
            bool started = false;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            Price avg = Average(updateTime, acceptMissingValues, linearInterpolation);
            IEnumerable<KeyValuePair<DateTime, Price>> generator = mktData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    if (!acceptMissingValues && Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalMilliseconds) != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                if (linearInterpolation)
                    var += ((beginPeriodValue.Value + endPeriodValue.Value) / 2m - avg).Abs() * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, (decimal)_periodSeconds);
                else
                    var += (beginPeriodValue.Value - avg).Abs() * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, (decimal)_periodSeconds);
                beginPeriodValue = endPeriodValue;
            }
            var += (beginPeriodValue.Value - avg).Abs() * _timeDecay.Weight(beginPeriodValue.Key, updateTime, (decimal)_periodSeconds);
            return var;
        }
    }

    /// <summary>
    /// Exponential Moving Average
    /// it gives more weight to recent data and as a result is more volatile than SMA
    /// 
    /// EMA = Price(t) * k + EMA(y) * (1 – k)
    /// with:
    ///     t = now, y = previous, N = number of periods in EMA, k = 2/(N+1)
    /// </summary>
    /// TODO: public class IndicatorEMA : IndicatorWMA

}
