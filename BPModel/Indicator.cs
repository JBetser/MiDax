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
            : base(id, new Dictionary<DateTime,L1LsPriceData>())
        {
            this._mktData = mktData;
        }

        public override void Subscribe(Tick eventHandler)
        {
            this._eventHandlers.Add(eventHandler);
            foreach (MarketData mktData in _mktData)
                mktData.Subscribe(OnUpdate);
        }    
    
        protected abstract void OnUpdate(MarketData mktData, DateTime time, L1LsPriceData value);
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

        protected override void OnUpdate(MarketData mktData, DateTime time, L1LsPriceData value)
        {
            if (mktData.Values.Count > 1)
                _values[time] = average(mktData);
        }

        L1LsPriceData average(MarketData mktData)
        {
            L1LsPriceData avg = new L1LsPriceData();
            DateTime lastTime = mktData.Values.Keys.ElementAt(mktData.Values.Keys.Count - 1);
            foreach (KeyValuePair<DateTime, L1LsPriceData> timeVal in mktData.Values)
            {
                if (timeVal.Key < lastTime)
                {
                    int nPeriod = _periodMinutes * 60 - (lastTime - timeVal.Key).Seconds;
                    if (nPeriod > 0)
                        avg.Bid += (timeVal.Value.Bid.Value - avg.Bid) / (nPeriod + 1);
                }
            }
            return avg;
        }
    }
}
