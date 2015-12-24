using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Indicator : MarketData
    {
        protected List<MarketData> _mktData = null;
        bool _publishingEnabled = true;

        public bool PublishingEnabled { get { return _publishingEnabled; } set { _publishingEnabled = value; } }

        public Indicator(string id, List<MarketData> mktData)
            : base(id)
        {
            _mktData = mktData;
        }

        public override void Subscribe(Tick eventHandler)
        {
            Clear();
            foreach (MarketData mktData in _mktData)
                mktData.Subscribe(OnUpdate);
            this._eventHandlers.Add(eventHandler);
        }

        public override void Unsubscribe(Tick eventHandler)
        {
            this._eventHandlers.Remove(eventHandler);
            foreach (MarketData mktData in _mktData)
                mktData.Unsubscribe(OnUpdate);            
        }

        protected virtual void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            _values.Add(updateTime, value);
            foreach (Tick ticker in this._eventHandlers)
                ticker(this, updateTime, value);
        }

        public override void Publish(DateTime updateTime, Price price)
        {
            if (price.Bid != price.Offer)
            {
                string error = "Inconsistent indicator " + _name + " values";
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_publishingEnabled)
                PublisherConnection.Instance.Insert(updateTime, this, price.Bid);
        }
    }

    public class IndicatorWMA : Indicator
    {
        int _periodSeconds;

        public MarketData Asset { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }
        
        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
        }

        public IndicatorWMA(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
        }

        public IndicatorWMA(IndicatorWMA indicator)
            : base(indicator.Id, new List<MarketData> { indicator.Asset })
        {
            _periodSeconds = indicator.Period;
        }

        // Whole day average
        public IndicatorWMA(MarketData mktData)
            : base("WMA_1D_" + mktData.Id, new List<MarketData> { mktData })
        {
            TimeSpan timeDiff = (DateTime.Parse(Config.Settings["PUBLISHING_STOP_TIME"]) - DateTime.Parse(Config.Settings["PUBLISHING_START_TIME"]));
            _periodSeconds = (timeDiff.Hours * 60 + timeDiff.Minutes) * 60 + timeDiff.Seconds;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.Count > 1)
            {
                Price avgPrice = Average(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }        

        public Price Average(MarketData mktData, DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            Price avg = new Price();
            bool started = false;
            int idxSecond = 0;
            int idxSecondStart = 0;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            decimal weight = (1m / (decimal)_periodSeconds) / 2m;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = mktData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    idxSecondStart = Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalSeconds);
                    if (!acceptMissingValues && idxSecondStart != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    idxSecond = idxSecondStart;
                    continue;
                }
                if (linearInterpolation)
                    avg += (beginPeriodValue.Value + endPeriodValue.Value) * weight * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                else
                    avg += beginPeriodValue.Value * weight * 2m * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                idxSecond += (int)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                beginPeriodValue = endPeriodValue;
            }
            avg += beginPeriodValue.Value * weight * 2m * (decimal)(updateTime - beginPeriodValue.Key).TotalSeconds;
            return avg;
        }
    }

    public class IndicatorWMVol : IndicatorWMA
    {
        int _periodSeconds;
        
        public IndicatorWMVol(MarketData mktData, int periodMinutes)
            : base("WMVol_" + periodMinutes + "_" + mktData.Id, mktData, periodMinutes)
        {
            _periodSeconds = periodMinutes * 60;
        }
        
        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.Count > 1)
            {
                Price avgPrice = AlgebricStdDev(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }

        public Price AlgebricStdDev(MarketData mktData, DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            Price var = new Price();
            bool started = false;
            int idxSecond = 0;
            int idxSecondStart = 0;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            decimal weight = (1m / (decimal)_periodSeconds);
            Price avg = Average( mktData, updateTime, acceptMissingValues, linearInterpolation);
            IEnumerable<KeyValuePair<DateTime, Price>> generator = mktData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    idxSecondStart = Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalSeconds);
                    if (!acceptMissingValues && idxSecondStart != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    idxSecond = idxSecondStart;
                    continue;
                }
                if (linearInterpolation)
                    var += ((beginPeriodValue.Value + endPeriodValue.Value) / 2m - avg) * weight * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                else
                    var += (beginPeriodValue.Value - avg) * weight * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                idxSecond += (int)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                beginPeriodValue = endPeriodValue;
            }
            var += (beginPeriodValue.Value - avg) * weight * (decimal)(updateTime - beginPeriodValue.Key).TotalSeconds;
            return var;
        }
    }

    public abstract class IndicatorLevel : IndicatorWMA
    {
        public IndicatorLevel(MarketData mktData)
            : base(mktData)
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        public abstract void Publish(DateTime updateTime);
    }

    public class IndicatorLevelMean : IndicatorLevel
    {
        public IndicatorLevelMean(MarketData mktData)
            : base(mktData)
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        public override void Publish(DateTime updateTime)
        {
            Price avg = Average(_mktData[0], updateTime, true);
            Publish(updateTime, avg.MidPrice());
        }

        public Price Average()
        {
            return Average(_mktData[0], DateTime.Parse(Config.Settings["PUBLISHING_STOP_TIME"]), true);
        }
    }
}
