using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace BPModel
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
            this._eventHandlers.Add(eventHandler);
            foreach (MarketData mktData in _mktData)
                mktData.Subscribe(OnUpdate);
        }

        protected abstract void OnUpdate(MarketData mktData, DateTime time, Price value);
    }

    public class IndicatorWMA : Indicator
    {
        const double SMOOTHING = 0.2;
        int _periodMinutes;

        public IndicatorWMA(string id, List<MarketData> mktData, int periodMinutes)
            : base(id, mktData)
        {
            _periodMinutes = periodMinutes;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.Values.Count > 1)
                _values.Add(updateTime, average(mktData, updateTime));
        }

        Price average(MarketData mktData, DateTime updateTime)
        {
            Price avg = new Price();
            decimal weight = (1m / (60m * _periodMinutes)) / 2m;
            for (int idxSecond = 0; idxSecond < 60 * _periodMinutes; idxSecond++)
                avg += (mktData.Values.Value(updateTime.AddSeconds(-1 * idxSecond)).Value.Value + mktData.Values.Value(updateTime.AddSeconds(-1 * (idxSecond+1))).Value.Value) * weight;
            return avg;
        }
    }
}
