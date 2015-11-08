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
        public Price(decimal b, decimal o, decimal v)
        {
            this.Bid = b;
            this.Offer = o;
            this.Volume = v;
        }
        public Price(decimal value)
        {
            this.Bid = value;
            this.Offer = value;
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
        public decimal Mid()
        {
            return (this.Bid + this.Offer) / 2m;
        }
        public Price MidPrice()
        {
            return new Price(Mid());
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
        public static Price operator /(Price p, decimal factor)
        {
            Price mult = new Price(p);
            mult.Bid /= factor;
            mult.Offer /= factor;
            return mult;
        }
    }

    public enum SIGNAL_CODE { UNKNOWN = 0, HOLD = 1, BUY = 2, SELL = 3 }

    public abstract class Signal
    {
        public delegate void Tick(Signal signal, DateTime time, Price value);

        protected string _id = null;
        protected string _name = null;
        protected MarketData _asset = null;
        protected Signal.Tick _onBuy = null;
        protected Signal.Tick _onSell = null;
        protected TimeSeries _values = null;
        protected List<Indicator> _mktIndicator = null;
        protected SIGNAL_CODE _signalCode = SIGNAL_CODE.UNKNOWN;

        public Signal(string id, MarketData asset)
        {
            this._id = id;
            this._name = id;
            this._asset = asset;
            this._mktIndicator = new List<Indicator>();
            this._values = new TimeSeries();
        }

        public void Subscribe(Signal.Tick onBuy, Signal.Tick onSell)
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

        public MarketData Asset
        {
            get { return _asset; }
        }

        void _onHold(Signal signal, DateTime updateTime, Price value)
        {
        }

        protected void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            SIGNAL_CODE oldSignalCode = _signalCode;
            Signal.Tick tradingOrder = _onHold;
            bool signaled = Process(mktData, updateTime, value, ref tradingOrder);
            if (signaled && _signalCode != oldSignalCode)
            {
                tradingOrder(this, updateTime, value);
                PublisherConnection.Instance.Insert(updateTime, this, _signalCode);
            }
        }

        protected abstract bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder);
    }
       
    public class SignalMacD : Signal
    {
        IndicatorWMA _low = null;
        IndicatorWMA _high = null;

        public IndicatorWMA IndicatorLow { get { return _low; } }
        public IndicatorWMA IndicatorHigh { get { return _high; } }

        public SignalMacD(MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : base("MacD_" + asset.Id + "_" + lowPeriod + "_" + highPeriod, asset)
        {
            _low = low == null ? new IndicatorWMA(asset, lowPeriod) : new IndicatorWMA(low);
            if (low != null)
                _low.PublishingEnabled = false;
            _high = high == null ? new IndicatorWMA(asset, highPeriod) : new IndicatorWMA(high);
            if (high != null)
                _high.PublishingEnabled = false;
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            KeyValuePair<DateTime, Price>? timeValueLow = _low.TimeSeries[updateTime];
            KeyValuePair<DateTime, Price>? timeValueHigh = _high.TimeSeries[updateTime];
            if (timeValueLow == null || timeValueHigh == null)
                return false;
            Price lowWMA = timeValueLow.Value.Value;
            Price highWMA = timeValueHigh.Value.Value;
            _values.Add(updateTime, lowWMA - highWMA);
            if (_values.Count > 1)
            {
                if (_values[updateTime].Value.Value.Offer > 0 && _signalCode != SIGNAL_CODE.BUY)
                {
                    tradingOrder = _onBuy;
                    _signalCode = SIGNAL_CODE.BUY;
                    return true;
                }
                else if (_values[updateTime].Value.Value.Bid < 0 && _signalCode != SIGNAL_CODE.SELL)
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
