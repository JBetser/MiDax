using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    class IndicatorCorrelation : Indicator
    {
        protected int _periodSeconds;
        protected IndicatorWMA _wma = null;
        protected IndicatorWMA _wmaRef = null;

        public MarketData MarketData { get { return _mktData[0]; } }
        public MarketData MarketDataRef { get { return _mktData[1]; } }
        public IndicatorWMA WMA { get { return _wma; } }
        public IndicatorWMA WMARef { get { return _wmaRef; } }
        public int Period { get { return _periodSeconds; } }

        public IndicatorCorrelation(IndicatorWMA wma, IndicatorWMA wmaRef, int periodMinutes)
            : base("Cor_" + periodMinutes + "_" + wma.MarketData.Id + "_" + wmaRef.MarketData.Id, new List<MarketData> { wma.MarketData, wmaRef.MarketData })
        {
            _wma = wma;
            _wmaRef = wmaRef;
            _periodSeconds = periodMinutes * 60;
        }

        public IndicatorCorrelation(string id, IndicatorWMA wma, IndicatorWMA wmaRef, int periodMinutes)
            : base(id, new List<MarketData> { wma.MarketData, wmaRef.MarketData })
        {
            _wma = wma;
            _wmaRef = wmaRef;
            _periodSeconds = periodMinutes * 60;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value, bool majorTick)
        {
            if (mktData.TimeSeries.TotalMinutes(updateTime) > (double)_periodSeconds / 60.0)
            {
                Price avgPrice = IndicatorFunc(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice, majorTick);
                    if (majorTick)
                        Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }

        protected Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            return Correlation(updateTime);
        }

        public Price Correlation(DateTime updateTime)
        {
            Price correl = new Price();
            bool started = false;
            int idxSecond = 0;
            int idxSecondStart = 0;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            decimal weight = (1m / (decimal)_periodSeconds) / 2m;
            Price avg = _wma.Average(updateTime);
            Price avgRef = _wmaRef.Average(updateTime);
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime, false);
            IEnumerable<KeyValuePair<DateTime, Price>> generatorRef = MarketDataRef.TimeSeries.ValueGenerator(startTime, updateTime, false);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            KeyValuePair<DateTime, Price> beginPeriodValueRef = new KeyValuePair<DateTime, Price>();
            KeyValuePair<DateTime, Price> endPeriodValueRef = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    idxSecondStart = Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalSeconds);
                    if (idxSecondStart != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    // find the matching period for the reference market data
                    foreach (var curendPeriodValueRef in generatorRef)
                    {
                        if (curendPeriodValueRef.Key > beginPeriodValue.Key)
                        {
                            endPeriodValueRef = curendPeriodValueRef;
                            break;
                        }
                        beginPeriodValueRef = curendPeriodValueRef;
                    }
                    idxSecond = idxSecondStart;
                    continue;
                }
                correl += calculateIntervalCorrelation(beginPeriodValue, beginPeriodValueRef, endPeriodValueRef,
                    endPeriodValue.Key, generator, generatorRef, avg, avgRef, weight);
                idxSecond += (int)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                beginPeriodValue = endPeriodValue;
            }
            correl += calculateIntervalCorrelation(beginPeriodValue, beginPeriodValueRef, endPeriodValueRef,
                    updateTime, generator, generatorRef, avg, avgRef, weight);
            return correl;
        }

        Price calculateIntervalCorrelation(KeyValuePair<DateTime, Price> beginPeriod, KeyValuePair<DateTime, Price> beginPeriodRef, KeyValuePair<DateTime, Price> endPeriodRef, DateTime updateTime,
            IEnumerable<KeyValuePair<DateTime, Price>> generator, IEnumerable<KeyValuePair<DateTime, Price>> generatorRef, 
            Price avg, Price avgRef, decimal weight)
        {
            var coeff = (beginPeriod.Value - avg) * weight * (decimal)(updateTime - beginPeriod.Key).TotalSeconds;
            var partialCorrel = ((beginPeriodRef.Value + endPeriodRef.Value) / 2m - avgRef).Abs() * weight * (decimal)(endPeriodRef.Key - beginPeriodRef.Key).TotalSeconds;
            var intervalCorrel = partialCorrel;
            if (endPeriodRef.Key < updateTime)
            {
                beginPeriodRef = endPeriodRef;
                foreach (var curendPeriodValueRef in generatorRef)
                {
                    if (curendPeriodValueRef.Key > updateTime)
                    {
                        intervalCorrel += ((beginPeriodRef.Value + curendPeriodValueRef.Value) / 2m - avgRef).Abs() * weight * (decimal)(updateTime - beginPeriodRef.Key).TotalSeconds;
                        beginPeriodRef = new KeyValuePair<DateTime, Price>(updateTime, beginPeriodRef.Value);
                        endPeriodRef = curendPeriodValueRef;
                        break;
                    }
                    intervalCorrel += ((beginPeriodRef.Value + curendPeriodValueRef.Value) / 2m - avgRef).Abs() * weight * (decimal)(curendPeriodValueRef.Key - beginPeriodRef.Key).TotalSeconds;
                    beginPeriodRef = curendPeriodValueRef;
                }
            }
            return coeff * intervalCorrel.Mid();
        }
    }
}
