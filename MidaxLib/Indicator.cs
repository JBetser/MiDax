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
        bool _subscribed = false;

        public bool PublishingEnabled { get { return _publishingEnabled; } set { _publishingEnabled = value; } }
        public MarketData SignalStock { get { return _mktData[0]; } }

        public Indicator(string id, List<MarketData> mktData)
            : base(id)
        {
            _mktData = mktData;
        }

        public override void Subscribe(Tick updateHandler, Tick tickerHandler)
        {
            Clear();
            if (!_subscribed)
            {
                _subscribed = true;
                foreach (MarketData mktData in _mktData)
                    mktData.Subscribe(OnUpdate, OnTick);
            }
            this._updateHandlers.Add(updateHandler);
        }

        public override void Unsubscribe(Tick updateHandler, Tick tickerHandler)
        {
            this._updateHandlers.Remove(updateHandler);
            if (_subscribed)
            {
                _subscribed = false;
                foreach (MarketData mktData in (from m in _mktData select m).Reverse())
                    mktData.Unsubscribe(OnUpdate, OnTick);
            }
        }

        protected virtual void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            _values.Add(updateTime, value);
        }

        public virtual void OnTick(MarketData mktData, DateTime updateTime, Price value)
        {
            foreach (Tick ticker in this._updateHandlers)
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

        public void Publish(DateTime updateTime, decimal price)
        {
            if (_publishingEnabled)
                PublisherConnection.Instance.Insert(updateTime, this, price);
        }
    }

    public class IndicatorWMA : Indicator
    {
        protected int _periodSeconds;

        public MarketData MarketData { get { return _mktData[0]; } }
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
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
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
            int idxSecond = 0;
            int idxSecondStart = 0;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            decimal weight = (1m / (decimal)_periodSeconds) / 2m;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
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
            int idxSecond = 0;
            int idxSecondStart = 0;
            DateTime startTime = updateTime.AddSeconds(-_periodSeconds);
            decimal weight = (1m / (decimal)_periodSeconds);
            Price avg = Average(updateTime, acceptMissingValues, linearInterpolation);
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
                    var += ((beginPeriodValue.Value + endPeriodValue.Value) / 2m - avg).Abs() * weight * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                else
                    var += (beginPeriodValue.Value - avg).Abs() * weight * (decimal)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                idxSecond += (int)(endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds;
                beginPeriodValue = endPeriodValue;
            }
            var += (beginPeriodValue.Value - avg).Abs() * weight * (decimal)(updateTime - beginPeriodValue.Key).TotalSeconds;
            return var;
        }
    }

    public class IndicatorNearestLevel : Indicator
    {
        public IndicatorNearestLevel(MarketData mktData)
            : base("NearestLevel_" + mktData.Id, new List<MarketData> { mktData }) { }

        public static decimal GetNearestLevel(decimal midPrice, MarketLevels mktLevels)
        {
            decimal referenceLevel = 0m;
            var diff = decimal.MaxValue;
            if (Math.Abs(midPrice - mktLevels.R3) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R3);
                referenceLevel = mktLevels.R3;
            }
            if (Math.Abs(midPrice - mktLevels.R2) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R2);
                referenceLevel = mktLevels.R2;
            }
            if (Math.Abs(midPrice - mktLevels.R1) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R1);
                referenceLevel = mktLevels.R1;
            }
            if (Math.Abs(midPrice - mktLevels.Pivot) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.Pivot);
                referenceLevel = mktLevels.Pivot;
            }
            if (Math.Abs(midPrice - mktLevels.S1) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S1);
                referenceLevel = mktLevels.S1;
            }
            if (Math.Abs(midPrice - mktLevels.S2) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S2);
                referenceLevel = mktLevels.S2;
            }
            if (Math.Abs(midPrice - mktLevels.S3) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S3);
                referenceLevel = mktLevels.S3;
            }
            return referenceLevel;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (!mktData.Levels.HasValue)
                return;
            Publish(updateTime, new Price(GetNearestLevel(value.Mid(), mktData.Levels.Value)));
        }
    }
}
