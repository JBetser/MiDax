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
            Log.Instance.WriteEntry(name_id, System.Diagnostics.EventLogEntryType.Information);
            this._id = name_id.Split(':').Count() > 1 ? name_id.Split(':')[1] : name_id;
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
                Log.Instance.WriteEntry("Update " + itemName + ": " + priceData.Bid + "/" + priceData.Offer, System.Diagnostics.EventLogEntryType.Information);
                foreach (var data in (from MarketData mktData in MarketData where itemName.Contains(mktData.Id) select mktData).ToList())
                    data.FireTick(DateTime.Parse(priceData.UpdateTime), priceData);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public void StartListening()
        {
            string [] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray()).TrimEnd(new char[]{',',' '});
            Log.Instance.WriteEntry("Subscribing to market data: " + epicsMsg + "...", System.Diagnostics.EventLogEntryType.Information);
            MarketDataTableKey = IGConnection.Instance.StreamClient.subscribeToMarketDetails(epics, this);            
        }

        public void StopListening()
        {
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray());            
            IGConnection.Instance.StreamClient.UnsubscribeTableKey(MarketDataTableKey);
            Log.Instance.WriteEntry("Unsubscribed to market data: " + epicsMsg, System.Diagnostics.EventLogEntryType.Information);
        }
    }
}
