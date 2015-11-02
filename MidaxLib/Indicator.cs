using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Indicator : MarketData
    {
        protected List<MarketData> _mktData = null;

        public Indicator(string id, List<MarketData> mktData)
            : base(id, new TimeSeries())
        {
            this._mktData = mktData;
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
    }

    public class IndicatorWMA : Indicator
    {
        int _periodMinutes;

        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA" + "_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodMinutes = periodMinutes;
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
            if (mktData.Values.Count > 1)
            {
                Price avgPrice = average(mktData, updateTime);
                if (avgPrice != null)
                {
                    _values.Add(updateTime, avgPrice);
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice);
                }
            }
        }

        public override void Publish(DateTime updateTime, Price price)
        {
            PublisherConnection.Instance.Insert(updateTime, this, (price.Bid + price.Offer) / 2m);
        }

        protected Price average(MarketData mktData, DateTime updateTime, bool acceptMissingValues = false, bool linearInterpolation = false)
        {
            Price avg = new Price();
            decimal weight = (1m / (60m * _periodMinutes)) / 2m;
            bool started = false;
            int idxSecondStart = 0;
            for (int idxSecond = 0; idxSecond < 60 * _periodMinutes; idxSecond++){
                KeyValuePair<DateTime, Price>? beginPeriodValue = mktData.Values.Value(updateTime.AddSeconds(-1 * (idxSecond + 1)));
                KeyValuePair<DateTime, Price>? endPeriodValue = mktData.Values.Value(updateTime.AddSeconds(-1 * idxSecond));
                if (!beginPeriodValue.HasValue || !endPeriodValue.HasValue)
                {
                    if (acceptMissingValues)
                    {
                        if (started)
                        {
                            decimal realWeight = (1m / (idxSecond - idxSecondStart + 1)) / 2m;
                            return avg * realWeight / weight;
                        }
                        else
                        {
                            weight = (1m / (60m * _periodMinutes - (idxSecond + 1))) / 2m;
                            continue;
                        }
                    }
                    else
                        return null;
                }
                if (linearInterpolation)
                    avg += (beginPeriodValue.Value.Value + endPeriodValue.Value.Value) * weight;
                else
                    avg += beginPeriodValue.Value.Value * weight * 2m;
                if (!started)
                {
                    started = true;
                    idxSecondStart = idxSecond;
                }
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
            PublisherConnection.Instance.Insert(updateTime, this, (avg.Bid + avg.Offer) / 2m);
        }
    }
}
