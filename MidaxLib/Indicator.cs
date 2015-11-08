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
            : base(id, new TimeSeries())
        {
            _mktData = mktData;
        }

        public override void Subscribe(Tick eventHandler)
        {            
            foreach (MarketData mktData in _mktData)
                mktData.Subscribe(OnUpdate);
            this._eventHandlers.Add(eventHandler);
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
        int _periodMinutes;

        public MarketData Asset { get { return _mktData[0]; } }
        public int Period { get { return _periodMinutes; } }
        
        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodMinutes = periodMinutes;
        }

        public IndicatorWMA(IndicatorWMA indicator)
            : base(indicator.Id, new List<MarketData> { indicator.Asset })
        {
            _periodMinutes = indicator.Period;
        }

        // Whole day average
        public IndicatorWMA(MarketData mktData)
            : base("WMA_1D_" + mktData.Id, new List<MarketData> { mktData })
        {
            TimeSpan timeDiff = (DateTime.Parse(Config.Settings["PUBLISHING_STOP_TIME"]) - DateTime.Parse(Config.Settings["PUBLISHING_START_TIME"]));
            _periodMinutes = timeDiff.Hours * 60 + timeDiff.Minutes;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.Count > 1)
            {
                Price avgPrice = average(mktData, updateTime);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }        

        protected Price average(MarketData mktData, DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = true)
        {
            Price avg = new Price();
            decimal weight = 0;
            bool started = false;
            int idxSecondStart = 0;
            for (int idxSecond = 0; idxSecond < 60 * _periodMinutes; idxSecond++){
                KeyValuePair<DateTime, Price>? beginPeriodValue = mktData.TimeSeries.Value(updateTime.AddSeconds(-1 * (idxSecond + 1)));
                KeyValuePair<DateTime, Price>? endPeriodValue = mktData.TimeSeries.Value(updateTime.AddSeconds(-1 * idxSecond));
                if (!beginPeriodValue.HasValue || !endPeriodValue.HasValue)
                {
                    if (acceptMissingValues)
                    {
                        if (started)
                        {
                            decimal realWeight = (1m / (idxSecond - idxSecondStart)) / 2m;
                            return avg * realWeight / weight;
                        }
                        else
                        {                            
                            continue;
                        }
                    }
                    else
                        return null;
                }
                if (!started)
                {
                    started = true;
                    idxSecondStart = idxSecond;
                    weight = (1m / (60m * _periodMinutes - idxSecond)) / 2m;
                }
                if (linearInterpolation)
                    avg += (beginPeriodValue.Value.Value + endPeriodValue.Value.Value) * weight;
                else
                    avg += beginPeriodValue.Value.Value * weight * 2m;                
            }
            return avg;
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
            Price avg = average(_mktData[0], updateTime, true);
            Publish(updateTime, avg.MidPrice());
        }
    }
}
