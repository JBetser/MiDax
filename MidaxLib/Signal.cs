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
        public decimal? Volume;
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
            this.Volume = cql.v;
        }
        public Price(L1LsPriceData priceData)
        {
            this.Bid = priceData.Bid.Value;
            this.Offer = priceData.Offer.Value;
            this.Volume = priceData.Volume;
        }
        public void set(decimal val)
        {
            this.Bid = val;
            this.Offer = val;
            this.Volume = 0;
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
            Price sum = new Price(p1);
            sum.Bid += p2;
            sum.Offer += p2;
            return sum;
        }
        public static Price operator -(Price p1, decimal p2)
        {
            Price diff = new Price(p1);
            diff.Bid -= p2;
            diff.Offer -= p2;
            return diff;
        }
        public static Price operator +(Price p1, Price p2)
        {
            Price sum = new Price(p1);
            sum += p2.Mid();
            return sum;
        }
        public static Price operator -(Price p1, Price p2)
        {
            Price sum = new Price(p1);
            sum -= p2.Mid();
            return sum;
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
        public static int comparison(Price p1, Price p2)
        {
            // precision is set to 1/10000 of bp
            return (int)(10000m * (p1.Mid() - p2.Mid()));
        }
        public static bool operator <=(Price p1, Price p2)
        {
            return comparison(p1, p2) <= 0;
        }
        public static bool operator <(Price p1, Price p2)
        {
            return comparison(p1, p2) < 0;
        }
        public static bool operator >=(Price p1, Price p2)
        {
            return comparison(p1, p2) >= 0;
        }
        public static bool operator >(Price p1, Price p2)
        {
            return comparison(p1, p2) > 0;
        } 
    }

    public enum SIGNAL_CODE { UNKNOWN = 0, HOLD = 1, BUY = 2, SELL = 3, FAILED = 4 }

    public abstract class Signal
    {
        public delegate bool Tick(Signal signal, DateTime time, Price value);
        public bool Enabled = false; 

        protected string _id = null;
        protected string _name = null;
        protected MarketData _asset = null;
        protected MarketData _tradingAsset = null;
        protected Signal.Tick _onBuy = null;
        protected Signal.Tick _onSell = null;
        protected Signal.Tick _onHold = null;
        protected List<Indicator> _mktIndicator = null;
        protected SIGNAL_CODE _signalCode = SIGNAL_CODE.UNKNOWN;
        protected Trade _lastTrade = null;
        bool _signalProcessing = false;

        public Signal(string id, MarketData asset, MarketData tradingAsset = null)
        {
            this._id = id;
            this._name = id;
            this._asset = asset;
            this._tradingAsset = tradingAsset == null ? asset : tradingAsset;
            this._mktIndicator = new List<Indicator>();
        }

        public void Subscribe(Signal.Tick onBuy, Signal.Tick onSell, Signal.Tick onHold)
        {
            _onBuy = onBuy;
            _onSell = onSell;
            _onHold = onHold == null ? OnHold : onHold;
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
        
        public MarketData TradingAsset
        {
            get { return _tradingAsset; }
        }

        public Trade Trade
        {
            get { return _lastTrade; }
            set { _lastTrade = value; }
        }

        public virtual void Reset(DateTime updateTime)
        {
        }

        protected bool OnHold(Signal signal, DateTime updateTime, Price value)
        {
            // hold your position; do nothing
            return false;
        }

        SIGNAL_CODE _oldSignalCode = SIGNAL_CODE.UNKNOWN;

        protected void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (_signalProcessing)
                return;
            lock (_mktIndicator)
            {
                if (_signalProcessing)
                    return;
                try
                {
                    _signalProcessing = true;
                    Signal.Tick tradingOrder = _onHold;
                    bool signaled = Process(mktData, updateTime, value, ref tradingOrder);
                    if (signaled)
                    {
                        // send a signal
                        var stockValue = _asset.TimeSeries[updateTime].Value.Value;
                        if (tradingOrder(this, updateTime, stockValue))
                        {
                            if (_signalCode == SIGNAL_CODE.BUY)
                            {
                                if (_oldSignalCode == SIGNAL_CODE.SELL)
                                    PublisherConnection.Instance.Insert(updateTime.AddSeconds(-1), this, _signalCode, stockValue.Offer);
                                PublisherConnection.Instance.Insert(updateTime, this, _signalCode, stockValue.Offer);
                            }
                            else if (_signalCode == SIGNAL_CODE.SELL)
                            {
                                if (_oldSignalCode == SIGNAL_CODE.BUY)
                                    PublisherConnection.Instance.Insert(updateTime.AddSeconds(-1), this, _signalCode, stockValue.Bid);
                                PublisherConnection.Instance.Insert(updateTime, this, _signalCode, stockValue.Bid);
                            }
                            else if (_signalCode == SIGNAL_CODE.HOLD)
                            {
                                if (_oldSignalCode == SIGNAL_CODE.BUY)
                                    PublisherConnection.Instance.Insert(updateTime, this, SIGNAL_CODE.SELL, stockValue.Bid);
                                else if (_oldSignalCode == SIGNAL_CODE.SELL)
                                    PublisherConnection.Instance.Insert(updateTime, this, SIGNAL_CODE.BUY, stockValue.Offer);
                            }
                            else if (_signalCode == SIGNAL_CODE.FAILED)
                                PublisherConnection.Instance.Insert(updateTime, this, _signalCode, stockValue.Bid);
                            _oldSignalCode = _signalCode;
                        }
                    }
                }
                finally
                {
                    _signalProcessing = false;
                }
            }
        }

        protected abstract bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder);
    }

    public class SignalALaCon : Signal
    {
        Signal.Tick _tradingOrder = null;

        public SignalALaCon(MarketData fx)
            : base("CON_" + fx.Id, fx)
        {
            _mktIndicator.Add(new IndicatorRSI(fx, 1, 14));
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            if (_tradingOrder == _onSell)
            {
                tradingOrder = _onBuy;
                _tradingOrder = _onBuy;
                return true;
            }
            else
            {
                tradingOrder = _onSell;
                _tradingOrder = _onSell;
                return true;
            }
        }
    }
}
