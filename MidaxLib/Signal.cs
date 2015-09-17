using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
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

    public enum SIGNAL_CODE { HOLD = 0, BUY = 1, SELL = 2 }

    public abstract class Signal
    {
        protected string _id = null;
        protected string _name = null;
        protected List<MarketData> _mktData = null;
        protected MarketData.Tick _onBuy = null;
        protected MarketData.Tick _onSell = null;
        protected TimeSeries _values = null;
        protected List<Indicator> _mktIndicator = null;
        protected SIGNAL_CODE _signalCode = SIGNAL_CODE.HOLD;

        public Signal(string id)
        {
            this._id = id;
            this._name = id;
            this._mktData = new List<MarketData>();
            this._mktIndicator = new List<Indicator>();
            this._values = new TimeSeries();
        }
        
        public void Subscribe(MarketData.Tick onBuy, MarketData.Tick onSell)
        {
            _onBuy = onBuy;
            _onSell = onSell;
            foreach (Indicator indicator in _mktIndicator)
                indicator.Subscribe(OnUpdate);
        }

        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public TimeSeries Values
        {
            get { return _values; }
        }

        protected void OnDoNothing(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        protected void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            SIGNAL_CODE oldSignalCode = _signalCode;
            MarketData.Tick tradingOrder = OnDoNothing;
            bool signaled = OnSignal(mktData, updateTime, value, ref tradingOrder);
            if (signaled && _signalCode != oldSignalCode)
            {
                tradingOrder(mktData, updateTime, value);
                PublisherConnection.Instance.Insert(updateTime, this, _signalCode);
            }
        }

        protected abstract bool OnSignal(MarketData mktData, DateTime updateTime, Price value, ref MarketData.Tick tradingOrder);
    }
       

    public class SignalMacD : Signal
    {
        IndicatorWMA _low = null;
        IndicatorWMA _high = null;

        public SignalMacD(MarketData mktData, int lowPeriod = 5, int highPeriod = 60)
            : base("MacD_" + mktData.Id)
        {
            _id += "_" + lowPeriod + "_" + highPeriod;
            _low = new IndicatorWMA(mktData, lowPeriod);
            _high = new IndicatorWMA(mktData, highPeriod);
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        protected override bool OnSignal(MarketData mktData, DateTime updateTime, Price value, ref MarketData.Tick tradingOrder)
        {
            Price lowWMA = _low.Values[updateTime].Value.Value;
            Price highWMA = _high.Values[updateTime].Value.Value;
            _values.Add(updateTime, lowWMA - highWMA);
            if (_values.Count > 1)
            {
                if (_values[updateTime].Value.Value.Offer > 0 && _values[updateTime.AddSeconds(-1)].Value.Value.Offer <= 0)
                {
                    tradingOrder = _onBuy;
                    _signalCode = SIGNAL_CODE.BUY;
                    return true;
                }
                else if (_values[updateTime].Value.Value.Bid < 0 && _values[updateTime.AddSeconds(-1)].Value.Value.Bid >= 0)
                {
                    tradingOrder = _onSell;
                    _signalCode = SIGNAL_CODE.SELL;
                    return true;
                }
            }
            return false;
        }
    }
}
