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
        public MarketData(string name_id, Dictionary<DateTime, L1LsPriceData> values)
        {
            this._id = name_id.Split(':')[1];
            this._name = name_id.Split(':')[0];
            this._values = values;
            this._eventHandlers = new List<Tick>();
        }

        public delegate void Tick(MarketData mktData, DateTime time, L1LsPriceData value);

        public virtual void Subscribe(Tick eventHandler)
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
                ticker(this, time, value);
        }

        protected Dictionary<DateTime, L1LsPriceData> _values;
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

        public Dictionary<DateTime, L1LsPriceData> Values
        {
            get { return _values; }
        }
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
            string [] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray());            
            MarketDataTableKey = IGConnection.Instance.StreamClient.subscribeToMarketDetails(epics, this);
            IGConnection.Instance.Log.WriteEntry("Subscribed to market data: " + epicsMsg, System.Diagnostics.EventLogEntryType.Information);
        }

        public void StopListening()
        {
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray());            
            IGConnection.Instance.StreamClient.UnsubscribeTableKey(MarketDataTableKey);
            IGConnection.Instance.Log.WriteEntry("Unsubscribed to market data: " + epicsMsg, System.Diagnostics.EventLogEntryType.Information);
        }
    }
}
