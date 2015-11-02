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
        protected string _id = null;
        protected string _name = null;
        protected MarketData _asset = null;
        protected MarketData.Tick _onBuy = null;
        protected MarketData.Tick _onSell = null;
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

        void _onHold(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        protected void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            SIGNAL_CODE oldSignalCode = _signalCode;
            MarketData.Tick tradingOrder = _onHold;
            bool signaled = OnSignal(mktData, updateTime, value, ref tradingOrder);
            if (signaled && _signalCode != oldSignalCode)
            {
                tradingOrder(_asset, updateTime, value);
                PublisherConnection.Instance.Insert(updateTime, this, _signalCode);
            }
        }

        protected abstract bool OnSignal(MarketData indicator, DateTime updateTime, Price value, ref MarketData.Tick tradingOrder);
    }
       
    public class SignalMacD : Signal
    {
        IndicatorWMA _low = null;
        IndicatorWMA _high = null;

        public SignalMacD(MarketData asset, int lowPeriod = 5, int highPeriod = 60)
            : base("MacD_" + asset.Id, asset)
        {
            _id += "_" + lowPeriod + "_" + highPeriod;
            _low = new IndicatorWMA(asset, lowPeriod);
            _high = new IndicatorWMA(asset, highPeriod);
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        protected override bool OnSignal(MarketData indicator, DateTime updateTime, Price value, ref MarketData.Tick tradingOrder)
        {
            KeyValuePair<DateTime, Price>? timeValueLow = _low.Values[updateTime];
            KeyValuePair<DateTime, Price>? timeValueHigh = _high.Values[updateTime];
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
