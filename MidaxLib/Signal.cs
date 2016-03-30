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
        public Price(CqlQuote cql)
        {
            this.Bid = cql.b.Value;
            this.Offer = cql.o.Value;
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
        public Price Abs()
        {
            return new Price(this.Bid >= 0m ? this.Bid : -this.Bid);
        }
        public static Price operator +(Price p1, decimal p2)
        {
            Price sum = new Price();
            sum.Bid = p1.Bid + p2;
            sum.Offer = p1.Offer + p2;
            return sum;
        }
        public static Price operator -(Price p1, decimal p2)
        {
            Price diff = new Price();
            diff.Bid = p1.Bid - p2;
            diff.Offer = p1.Offer - p2;
            return diff;
        }
        public static Price operator +(Price p1, Price p2)
        {
            return p1 + p2.Mid();
        }
        public static Price operator -(Price p1, Price p2)
        {
            return p1 - p2.Mid();
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

    public enum SIGNAL_CODE { UNKNOWN = 0, HOLD = 1, BUY = 2, SELL = 3, FAILED = 4 }

    public abstract class Signal
    {
        public delegate bool Tick(Signal signal, DateTime time, Price value);

        protected string _id = null;
        protected string _name = null;
        protected MarketData _asset = null;
        protected Signal.Tick _onBuy = null;
        protected Signal.Tick _onSell = null;
        protected List<Indicator> _mktIndicator = null;
        protected SIGNAL_CODE _signalCode = SIGNAL_CODE.UNKNOWN;
        protected Trade _lastTrade = null;
        
        public Signal(string id, MarketData asset)
        {
            this._id = id;
            this._name = id;
            this._asset = asset;
            this._mktIndicator = new List<Indicator>();
        }

        public void Subscribe(Signal.Tick onBuy, Signal.Tick onSell)
        {
            _onBuy = onBuy;
            _onSell = onSell;
            foreach (Indicator indicator in _mktIndicator)
                indicator.Subscribe(OnUpdate, null);
        }

        public void Unsubscribe()
        {
            _onBuy = null;
            _onSell = null;
            foreach (Indicator indicator in (from i in _mktIndicator select i).Reverse())
                indicator.Unsubscribe(OnUpdate, null);
        }
        
        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }
        
        public MarketData MarketData
        {
            get { return _asset; }
        }

        public Trade Trade
        {
            get { return _lastTrade; }
            set { _lastTrade = value; }
        }

        protected bool _onHold(Signal signal, DateTime updateTime, Price value)
        {
            // hold your position; do nothing
            return true;
        }

        protected void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {            
            SIGNAL_CODE oldSignalCode = _signalCode;
            Signal.Tick tradingOrder = _onHold;
            bool signaled = Process(mktData, updateTime, value, ref tradingOrder);
            if (signaled && _signalCode != oldSignalCode)
            {
                // send a signal
                var stockValue = MarketData.TimeSeries[updateTime].Value.Value;
                if (tradingOrder(this, updateTime, stockValue))
                {
                    if (_signalCode == SIGNAL_CODE.BUY)
                        PublisherConnection.Instance.Insert(updateTime, this, _signalCode, stockValue.Offer);
                    else
                        PublisherConnection.Instance.Insert(updateTime, this, _signalCode, stockValue.Bid);
                }
            }
        }

        protected abstract bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder);
    }
       
    public class SignalMacD : Signal
    {
        protected IndicatorWMA _low = null;
        protected IndicatorWMA _high = null;
        protected SIGNAL_CODE _trendAssumption = SIGNAL_CODE.UNKNOWN;

        public IndicatorWMA IndicatorLow { get { return _low; } }
        public IndicatorWMA IndicatorHigh { get { return _high; } }

        public SignalMacD(MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : base("MacD_" + lowPeriod + "_" + highPeriod + "_" + asset.Id, asset)
        {
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trendAssumption = Config.Settings["ASSUMPTION_TREND"] == "BULL" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL;            
            _low = low == null ? new IndicatorWMA(asset, lowPeriod) : new IndicatorWMA(low);
            if (low != null)
                _low.PublishingEnabled = false;
            _high = high == null ? new IndicatorWMA(asset, highPeriod) : new IndicatorWMA(high);
            if (high != null)
                _high.PublishingEnabled = false;
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        public SignalMacD(string id, MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : this(asset, lowPeriod, highPeriod, low, high)
        {
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trendAssumption = Config.Settings["ASSUMPTION_TREND"] == "BULL" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL;            
            _id = id;
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            KeyValuePair<DateTime, Price>? timeValueLow = _low.TimeSeries[updateTime];
            KeyValuePair<DateTime, Price>? timeValueHigh = _high.TimeSeries[updateTime];
            if (timeValueLow == null || timeValueHigh == null)
                return false;
            Price lowWMA = timeValueLow.Value.Value;
            Price highWMA = timeValueHigh.Value.Value;
            var signalValue = lowWMA - highWMA;
            var tradeTrigger = _signalCode != SIGNAL_CODE.UNKNOWN && _signalCode != SIGNAL_CODE.HOLD;
            if (_signalCode == SIGNAL_CODE.UNKNOWN)
            {
                tradingOrder = _onHold;
                _signalCode = SIGNAL_CODE.HOLD;
                return false;
            }
            else if (signalValue.Offer > 0)
            {
                tradingOrder = _onBuy;
                _signalCode = SIGNAL_CODE.BUY;
                return tradeTrigger;
            }
            else if (signalValue.Bid < 0)
            {
                tradingOrder = _onSell;
                _signalCode = SIGNAL_CODE.SELL;
                return tradeTrigger;
            }
            return false;
        }
    }
}
