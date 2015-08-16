using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace BPModel
{
    public class Price
    {
        public decimal Bid;
        public decimal Offer;
        public decimal Volume;
        public Price()
        { 
            this.Bid = 0;
            this.Offer = 0;
            this.Volume = 0;
        }
        public Price(Price cpy)
        {
            this.Bid = cpy.Bid;
            this.Offer = cpy.Offer;
            this.Volume = cpy.Volume;
        }
        public Price(L1LsPriceData priceData)
        {
            this.Bid = priceData.Bid.Value;
            this.Offer = priceData.Offer.Value;
        }
        public static Price operator +(Price p1, Price p2)
        {
            Price sum = new Price();
            sum.Bid = p1.Bid + p2.Bid;
            sum.Offer = p1.Offer + p2.Offer;
            return sum;
        }
        public static Price operator -(Price p1, Price p2)
        {
            Price diff = new Price();
            diff.Bid = p1.Bid - p2.Bid;
            diff.Offer = p1.Offer - p2.Offer;
            return diff;
        }
        public static Price operator *(Price p, decimal factor)
        {
            Price mult = new Price(p);
            mult.Bid *= factor;
            mult.Offer *= factor;
            return mult;
        }
    }

    public abstract class Signal
    {
        List<MarketData> _mktData = null;
        protected MarketData.Tick _onBuy = null;
        protected MarketData.Tick _onSell = null;
        protected TimeSeries _values = null;
        protected List<Indicator> _mktIndicator = null;

        public Signal()
        {
            this._mktData = new List<MarketData>();
            this._mktIndicator = new List<Indicator>();
            this._values = new TimeSeries();
        }
        
        public void Subscribe(MarketData.Tick onBuy, MarketData.Tick onSell)
        {
            _onBuy = onBuy;
            _onSell = onSell;
            foreach (MarketData mktData in _mktData)
                mktData.Subscribe(OnUpdate);
            foreach (Indicator indicator in _mktIndicator)
                indicator.Subscribe(OnUpdate);
        }

        public TimeSeries Values
        {
            get { return _values; }
        }

        protected abstract void OnUpdate(MarketData mktData, DateTime time, Price value);
    }
       

    public class SignalMacD : Signal
    {
        IndicatorWMA _low = null;
        IndicatorWMA _high = null;

        public SignalMacD(List<MarketData> mktData, int lowPeriod = 5, int highPeriod = 60)
        {
            _low = new IndicatorWMA("WMA_Low", mktData, lowPeriod);
            _high = new IndicatorWMA("WMA_High", mktData, highPeriod);
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        protected override void OnUpdate(MarketData mktData, DateTime time, Price value)
        {
            Price lowWMA = _low.Values[time].Value.Value;
            Price highWMA = _high.Values[time].Value.Value;
            _values.Add(time, lowWMA - highWMA);
            if (_values.Count > 1)
            {
                if (_values[time].Value.Value.Offer > 0 && _values[time.AddSeconds(-1)].Value.Value.Offer <= 0)
                {
                    _onBuy(mktData, time, value);
                }
                else if (_values[time].Value.Value.Bid < 0 && _values[time.AddSeconds(-1)].Value.Value.Bid >= 0)
                {
                    _onSell(mktData, time, value);
                }
            }
        }
    }
}
