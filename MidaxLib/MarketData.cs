using System;
using System.Collections.Generic;
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
        public MarketLevels? Levels { get { return _marketLevels; } set { _marketLevels = value; } }

        public MarketData(string name_id)
        {
            this._id = name_id.Split(':').Count() > 1 ? name_id.Split(':')[1] : name_id;
            this._name = name_id.Split(':')[0];
            this._values = new TimeSeries();
            this._eventHandlers = new List<Tick>();
            if (!_replay.HasValue)
                _replay = Config.Settings["TRADING_MODE"] == "REPLAY";
        }

        public delegate void Tick(MarketData mktData, DateTime time, Price value);

        public virtual void Subscribe(Tick eventHandler)
        {
            Clear();
            bool subscribe = (this._eventHandlers.Count == 0);
            this._eventHandlers.Add(eventHandler);
            if (subscribe)
                MarketDataConnection.Instance.SubscribeMarketData(this);
        }

        public virtual void Unsubscribe(Tick eventHandler)
        {
            this._eventHandlers.Remove(eventHandler);
            if (this._eventHandlers.Count == 0)
                MarketDataConnection.Instance.UnsubscribeMarketData(this);
        }

        public void Clear()
        {
            this._values = new TimeSeries();
        }

        public void FireTick(DateTime updateTime, L1LsPriceData value)
        {
            Price livePrice = new Price(value);
            if (!_replay.Value || value.MarketState == "REPLAY")
                _values.Add(updateTime, livePrice);
            foreach (Tick ticker in this._eventHandlers)
                ticker(this, updateTime, livePrice);
            Publish(updateTime, livePrice);
        }

        public virtual void Publish(DateTime updateTime, Price price)
        {
            PublisherConnection.Instance.Insert(updateTime, this, price);
        }

        public void GetMarketLevels()
        {
            if (PublisherConnection.Instance.Database != null)
                _marketLevels = PublisherConnection.Instance.Database.GetMarketLevels(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]), _id);
        }

        protected TimeSeries _values;
        protected string _id;
        protected string _name;
        protected List<Tick> _eventHandlers;

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

        public MarketLevels(string assetId, decimal low, decimal high, decimal closeBid, decimal closeOffer)
        {
            AssetId = assetId;
            Low = low;
            High = high;
            CloseBid = closeBid;
            CloseOffer = closeOffer;
            CloseMid = (CloseBid + CloseOffer) / 2m;
            Pivot = (High + Low + CloseMid) / 3m;
            R1 = 2m * Pivot - Low;
            S1 = 2m * Pivot - High;
            R2 = Pivot + (High - Low);
            S2 = Pivot - (High - Low);
            R3 = R1 + (High - Low);
            S3 = S1 - (High - Low);
        }
    }
}
