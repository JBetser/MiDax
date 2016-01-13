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
                    _tradeSubscriptionStk = _igStreamApiClient.SubscribeToPositions(this);
                    Log.Instance.WriteEntry("TradeSubscription : Subscribe");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry("Portfolio subscription error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }
        
        public void ClosePosition(Trade trade, DateTime time)
        {
            _igStreamApiClient.ClosePosition(trade, time, OnTradeBooked);
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
            if (!_positions.ContainsKey(newTrade.Epic))
                _positions.Add(newTrade.Epic, new Position(newTrade.Epic));
            _igStreamApiClient.BookTrade(newTrade, OnTradeBooked);
        }

        public Position GetPosition(string epic)
        {
            if (!_positions.ContainsKey(epic))
                _positions.Add(epic, new Position(epic));
            return _positions[epic];
        }

        void IHandyTableListener.OnRawUpdatesLost(int itemPos, string itemName, int lostUpdates)
        {
            foreach (var item in _positions)
                item.Value.OnRawUpdatesLost(lostUpdates);
        }

        void IHandyTableListener.OnSnapshotEnd(int itemPos, string itemName)
        {
            foreach (var item in _positions)
                item.Value.OnSnapshotEnd();
        }

        void IHandyTableListener.OnUnsubscr(int itemPos, string itemName)
        {
            foreach (var item in _positions)
                item.Value.OnUnsubscr();
        }

        void IHandyTableListener.OnUnsubscrAll()
        {
            foreach (var item in _positions)
                item.Value.OnUnsubscrAll();
        }

        void IHandyTableListener.OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            foreach (var item in _positions)
                item.Value.OnUpdate(update);
        }
    }
}
