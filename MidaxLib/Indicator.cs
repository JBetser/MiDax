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
        List<MarketData> _mktData = null;

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

        protected abstract void OnUpdate(MarketData mktData, DateTime time, Price value);
    }

    public class IndicatorWMA : Indicator
    {
        const double SMOOTHING = 0.2;
        int _periodMinutes;

        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA" + "_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodMinutes = periodMinutes;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.Values.Count > 1)
            {
                Price avgPrice = average(mktData, updateTime);
                if (avgPrice != null)
                {
                    _values.Add(updateTime, avgPrice);
                    Publish(updateTime, avgPrice);
                }
            }
        }

        public override void Publish(DateTime updateTime, Price price)
        {
            PublisherConnection.Instance.Insert(updateTime, this, (price.Bid + price.Offer) / 2m);
        }

        Price average(MarketData mktData, DateTime updateTime)
        {
            Price avg = new Price();
            decimal weight = (1m / (60m * _periodMinutes)) / 2m;
            for (int idxSecond = 0; idxSecond < 60 * _periodMinutes; idxSecond++){
                KeyValuePair<DateTime,Price>? lastPeriodValue = mktData.Values.Value(updateTime.AddSeconds(-1 * (idxSecond+1)));
                if (!lastPeriodValue.HasValue)
                    return null;
                avg += (mktData.Values.Value(updateTime.AddSeconds(-1 * idxSecond)).Value.Value + lastPeriodValue.Value.Value) * weight;
            }
            return avg;
        }
    }
}
