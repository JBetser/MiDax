using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class Portfolio : IHandyTableListener
    {
        Dictionary<string, Position> _positions = new Dictionary<string, Position>();
        List<Trade> _trades = new List<Trade>();
        IAbstractStreamingClient _igStreamApiClient = null;
        SubscribedTableKey _tradeSubscriptionStk = null;

        public Dictionary<string, Position> Positions { get { return _positions; } }
        
        public Portfolio(IAbstractStreamingClient client)
        {
            _igStreamApiClient = client;
            try
            {
                if (_igStreamApiClient != null)
                {
                    //_tradeSubscriptionStk = _igStreamApiClient.SubscribeToTradeSubscription(this);
                    Log.Instance.WriteEntry("TradeSubscription : Subscribe");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public Position Position(string stockid)
        {
            if (!_positions.ContainsKey(stockid))
                _positions.Add(stockid, new Position(stockid));
            return _positions[stockid];
        }

        public void ClosePosition(Trade trade)
        {
            _igStreamApiClient.ClosePosition(trade, OnTradeBooked);
        }

        public void Unsubscribe()
        {
            if (_tradeSubscriptionStk != null)
            {
                _igStreamApiClient.UnsubscribeTradeSubscription(_tradeSubscriptionStk);
                Log.Instance.WriteEntry("TradeSubscription : Unsubscribe");
            }
        }

        public delegate void TradeBookedEvent(Trade newTrade);

        void OnTradeBooked(Trade newTrade)
        {
            _trades.Add(newTrade);
            GetPosition(newTrade.Epic).Trade = newTrade;
            newTrade.Publish();                
        }
        
        public void BookTrade(Trade newTrade)
        {
            if (newTrade == null)
                return;
            _igStreamApiClient.BookTrade(newTrade, OnTradeBooked);
        }

        Position GetPosition(string itemName)
        {
            if (!_positions.ContainsKey(itemName))
                _positions.Add(itemName, new Position(itemName));
            return _positions[itemName];
        }

        void IHandyTableListener.OnRawUpdatesLost(int itemPos, string itemName, int lostUpdates)
        {
            GetPosition(itemName).OnRawUpdatesLost(itemPos, lostUpdates);
        }

        void IHandyTableListener.OnSnapshotEnd(int itemPos, string itemName)
        {
            GetPosition(itemName).OnSnapshotEnd(itemPos);
        }

        void IHandyTableListener.OnUnsubscr(int itemPos, string itemName)
        {
            GetPosition(itemName).OnUnsubscr(itemPos);
        }

        void IHandyTableListener.OnUnsubscrAll()
        {
            foreach (var item in _positions)
                item.Value.OnUnsubscrAll();
        }

        void IHandyTableListener.OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            GetPosition(itemName).OnUpdate(itemPos, update);
        }
    }
}
