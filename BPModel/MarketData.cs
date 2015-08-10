using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;

namespace BPModel
{    
    public class MarketData
    {
        public MarketData(string id, Dictionary<DateTime, L1LsPriceData> values)
        {
            this._id = id;
            this._values = values;
            this._eventHandlers = new List<Tick>();
        }

        public delegate void Tick(string id, DateTime time, L1LsPriceData value);

        public void Subscribe(Tick eventHandler)
        {
            bool subscribe = (this._eventHandlers.Count == 0);
            this._eventHandlers.Add(eventHandler);
            if (subscribe)
                IGConnection.Instance.SubscribeMarketData(this); 
        }

        public void FireTick(DateTime time, L1LsPriceData value)
        {
            _values[time] = value;
            foreach (Tick ticker in this._eventHandlers)
                ticker(_id, time, value);
        }

        protected Dictionary<DateTime, L1LsPriceData> _values;
        private string _id;
        private List<Tick> _eventHandlers;

        public string Id
        {
            get { return _id; }
        }

        public Dictionary<DateTime, L1LsPriceData> Values
        {
            get { return _values; }
        }
    }

    public class CompositeData : MarketData
    {
    }

    public class MarketDataSubscription : HandyTableListenerAdapter
    {
        public List<MarketData> MarketData = new List<MarketData>();
        public SubscribedTableKey MarketDataTableKey = null;

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                L1LsPriceData priceData = L1LsPriceUpdateData(itemPos, itemName, update);
                IGConnection.Instance.Log.WriteEntry(priceData.ToString(), System.Diagnostics.EventLogEntryType.Information);
                foreach (var data in (from MarketData mktData in MarketData where mktData.Id == itemName select mktData).ToList())
                    data.FireTick(DateTime.Parse(priceData.UpdateTime), priceData);
            }
            catch (Exception ex)
            {
                IGConnection.Instance.Log.WriteEntry(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public void StartListening()
        {
            MarketDataTableKey = IGConnection.Instance.StreamClient.subscribeToMarketDetails((from MarketData mktData in MarketData select mktData.Id).ToArray(), this);
        }
    }
}
