using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dto.endpoint.search;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class MarketData
    {
        static bool? _replay = null;

        MarketLevels? _marketLevels = null;
        protected DateTime _closePositionTime;
        protected DateTime _lastUpdateTime = DateTime.MinValue;
        Price _lastUpdatePrice = new Price();
        protected bool _allPositionsClosed;
        protected string _mktLevelsId;
        public MarketLevels? Levels { get { return _marketLevels; } set { _marketLevels = value; } }
        public bool _hasEodLevels = true;
        protected bool _isReady = false;

        public bool HasEODLevels { get { return _hasEodLevels; } }
        public bool Ready { get { return _isReady; } set { _isReady = value; } }

        public MarketData(string name_id, string mktLevelsId = "")
        {
            _id = name_id.Split(':').Count() > 1 ? name_id.Split(':')[1] : name_id;
            _mktLevelsId = mktLevelsId == "" ? _id : (mktLevelsId.Split(':').Count() > 1 ? mktLevelsId.Split(':')[1] : mktLevelsId);
            _name = name_id.Split(':')[0];
            _values = new TimeSeries();
            _updateHandlers = new List<Tick>();
            _tickHandlers = new List<Tick>();
            _allPositionsClosed = false;
            if (name_id.Contains("VIX"))
                _hasEodLevels = false;
            if (!_replay.HasValue)
                _replay = Config.Settings["TRADING_MODE"] == "REPLAY";
        }

        public delegate void Tick(MarketData mktData, DateTime time, Price value);

        public virtual void Subscribe(Tick updateHandler, Tick tickerHandler)
        {
            _lastUpdateTime = DateTime.MinValue;
            _allPositionsClosed = false;
            _closePositionTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_STOP_TIME"]);
            if (updateHandler != null)
                _updateHandlers.Add(updateHandler);
            if (tickerHandler != null)
                _tickHandlers.Add(tickerHandler);
            MarketDataConnection.Instance.SubscribeMarketData(this);
        }

        public virtual void Unsubscribe(Tick updateHandler, Tick tickerHandler)
        {
            _updateHandlers.Remove(updateHandler);
            if (tickerHandler != null)
                _updateHandlers.Remove(tickerHandler);
            if (this._updateHandlers.Count == 0)
            {
                MarketDataConnection.Instance.UnsubscribeMarketData(this);
                if (!_allPositionsClosed)
                {
                    Portfolio.Instance.CloseAllPositions(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
                    _allPositionsClosed = true;
                }
            }
        }

        public void Clear()
        {
            _values = new TimeSeries();
            _updateHandlers.Clear();
            _tickHandlers.Clear();
        }

        public void FireTick(DateTime updateTime, L1LsPriceData value)
        {
            FireTick(updateTime, new Price(value));
        }

        public void FireTick(DateTime updateTime, Price value)
        {
            if (!_isReady)
                return;
            _values.Add(updateTime, value);
            if (updateTime > _closePositionTime && !_allPositionsClosed)
            {
                Portfolio.Instance.CloseAllPositions(updateTime);
                _allPositionsClosed = true;
            }
            foreach (Tick ticker in this._updateHandlers)
                ticker(this, updateTime, value);
            foreach (Tick ticker in this._tickHandlers)
                ticker(this, updateTime, value);
            Publish(updateTime, value);
            /*
            var curPeriodSec = (decimal)(updateTime - _lastUpdateTime).TotalSeconds;
            _values.Add(updateTime, value);
            if (curPeriodSec >= 1m)
            {
                FireTick(updateTime, value, true);
                Publish(updateTime, value);
                _lastUpdatePrice = new Price(value);
                _lastUpdatePrice.Volume = 0m;
                _lastUpdateTime = updateTime;
            }
            else
            {
                _lastUpdatePrice.Bid = value.Bid;
                _lastUpdatePrice.Offer = value.Offer;
                _lastUpdatePrice.Volume += value.Volume;
                FireTick(updateTime, value);
                Publish(updateTime, value);
            }*/
        }

        /*
        public void FireTick(DateTime updateTime, Price livePrice)
        {
            if (updateTime > _closePositionTime && !_allPositionsClosed)
            {
                Portfolio.Instance.CloseAllPositions(updateTime);
                _allPositionsClosed = true;
            }
            foreach (Tick ticker in this._updateHandlers)
                ticker(this, updateTime, livePrice);
            foreach (Tick ticker in this._tickHandlers)
                ticker(this, updateTime, livePrice);            
        }*/

        public virtual void Publish(DateTime updateTime, Price price)
        {
            PublisherConnection.Instance.Insert(updateTime, this, price);
        }

        public void GetMarketLevels()
        {
            try
            {
                if (PublisherConnection.Instance.Database != null)
                {
                    var mktLevels = PublisherConnection.Instance.Database.GetMarketLevels(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]),
                        new List<string> { _mktLevelsId });
                    if (mktLevels.Count == 1)
                        _marketLevels = new MarketLevels(mktLevels.Values.First());
                    else
                        Log.Instance.WriteEntry("Could not retrieve market levels for Market Data: " + _mktLevelsId, EventLogEntryType.Warning);
                }
            }
            catch
            {
                Log.Instance.WriteEntry("Could not retrieve market levels for Market Data: " + _mktLevelsId, EventLogEntryType.Warning);
            }
        }

        protected TimeSeries _values;
        protected string _id;
        protected string _name;
        protected List<Tick> _updateHandlers;
        protected List<Tick> _tickHandlers;

        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public TimeSeries TimeSeries
        {
            get { return _values; }
            set { _values = value; }
        }
    }

    public struct MarketLevels
    {
        public string AssetId;
        public decimal Low;
        public decimal High;
        public decimal CloseBid;
        public decimal CloseOffer;
        public decimal CloseMid;
        public decimal Pivot;
        public decimal R1;
        public decimal R2;
        public decimal R3;
        public decimal S1;
        public decimal S2;
        public decimal S3;

        public bool Available { get { return AssetId != ""; } }
        public void SetAvailable(bool val)
        {
            AssetId = val ? AssetId : "";
        }

        public MarketLevels(string assetId, decimal low, decimal high, decimal closeBid, decimal closeOffer)
        {
            AssetId = assetId;
            Low = low;
            High = high;
            CloseBid = closeBid;
            CloseOffer = closeOffer;
            if ((Low > decimal.MinValue && Low < decimal.MaxValue) &&
                (High > decimal.MinValue && High < decimal.MaxValue) &&
                (CloseBid > decimal.MinValue && CloseBid < decimal.MaxValue) &&
                (CloseOffer > decimal.MinValue && CloseOffer < decimal.MaxValue))
            {
                CloseMid = (CloseBid + CloseOffer) / 2m;
                Pivot = (High + Low + CloseMid) / 3m;
                R1 = 2m * Pivot - Low;
                S1 = 2m * Pivot - High;
                R2 = Pivot + (High - Low);
                S2 = Pivot - (High - Low);
                R3 = R1 + (High - Low);
                S3 = S1 - (High - Low);
            }
            else
            {
                CloseMid = Pivot = R1 = S1 = R2 = S2 = R3 = S3 = 0m;
            }      
        }

        public MarketLevels(MarketLevels cpy) : this(cpy.AssetId, cpy.Low, cpy.High, cpy.CloseBid,
                                                cpy.CloseOffer)
        {
        }
    }
}
