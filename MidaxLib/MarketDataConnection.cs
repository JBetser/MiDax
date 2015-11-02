using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public interface IAbstractStreamingClient
    {
        void Connect(string username, string password, string apiKey);
        void Subscribe(string[] epics, IHandyTableListener tableListener);
        void Unsubscribe();
    }

    public abstract class MarketDataConnection
    {
        protected MarketDataSubscription _mktDataListener = null;
        static MarketDataConnection _instance = null;
        protected IAbstractStreamingClient _apiStreamingClient = null;
        protected TimerCallback _callbackConnectionClosed = null;

        protected MarketDataConnection() {
            _mktDataListener = new MarketDataSubscription();             
        }

        protected MarketDataConnection(IAbstractStreamingClient iclient)
        {
            _apiStreamingClient = iclient;
            _mktDataListener = new MarketDataSubscription();
        }
        
        static public MarketDataConnection Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                if (Config.ReplayEnabled)
                    _instance = new ReplayConnection();
                else if (Config.TradingEnabled)
                    _instance = new IGConnection();
                return _instance;
            }
        }

        public abstract void Connect(TimerCallback connectionClosed);
        
        public IAbstractStreamingClient StreamClient
        {
            get { return _apiStreamingClient; }
        }

        public void SubscribeMarketData(MarketData mktData)
        {
            _mktDataListener.MarketData.Add(mktData);
            Log.Instance.WriteEntry("Subscribed " + mktData.Name + " to " + mktData.Id);
        }

        public virtual void StartListening()
        {
            _mktDataListener.StartListening();
        }

        public virtual void StopListening()
        {
            _mktDataListener.StopListening();
        }        
    }

    public class MarketDataSubscription : HandyTableListenerAdapter
    {
        public List<MarketData> MarketData = new List<MarketData>();
        public SubscribedTableKey MarketDataTableKey = null;
        
        public void StartListening()
        {
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray()).TrimEnd(new char[] { ',', ' ' });
            Log.Instance.WriteEntry("Subscribing to market data: " + epicsMsg + "...", System.Diagnostics.EventLogEntryType.Information);
            MarketDataConnection.Instance.StreamClient.Subscribe(epics, this);
        }

        public void StopListening()
        {
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray());
            Log.Instance.WriteEntry("Unsubscribing to market data: " + epicsMsg + "...", System.Diagnostics.EventLogEntryType.Information);
            MarketDataConnection.Instance.StreamClient.Unsubscribe();            
        }

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                if (Config.PublishingOpen)
                {
                    L1LsPriceData priceData = L1LsPriceUpdateData(itemPos, itemName, update);
                    if (priceData.MarketState == "TRADEABLE")
                    {
                        foreach (var data in (from MarketData mktData in MarketData where itemName.Contains(mktData.Id) select mktData).ToList())
                            data.FireTick(DateTime.Parse(priceData.UpdateTime), priceData);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                throw;
            }
        }
    }
}
