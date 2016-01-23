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
        static Dictionary<IAbstractStreamingClient, Portfolio> _instance = null;
        Dictionary<string, TradingSet> _tradingSets = new Dictionary<string, TradingSet>();

        public List<Trade> Trades { get { return _trades; } }
        public Dictionary<string, Position> Positions { get { return _positions; } }
        
        Portfolio(IAbstractStreamingClient client)
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

        public static Portfolio Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Dictionary<IAbstractStreamingClient, Portfolio>();
                if (!_instance.ContainsKey(MarketDataConnection.Instance.StreamClient))
                    _instance[MarketDataConnection.Instance.StreamClient] = new Portfolio(MarketDataConnection.Instance.StreamClient);
                return _instance[MarketDataConnection.Instance.StreamClient];
            }
        }
        
        public void ClosePosition(Trade trade, DateTime time)
        {
            _igStreamApiClient.ClosePosition(trade, time, OnTradeBooked);
        }

        public void CloseAllPositions(DateTime time, string stockid = "")
        {
            foreach (var position in Positions)
            {
                if (position.Value.Quantity != 0)
                {
                    if (stockid == "" || stockid == position.Value.Trade.Epic)
                        ClosePosition(position.Value.Trade, time);
                }
            }
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
            if (Config.TradingOpen(newTrade.TradingTime))
            {
                if (!_positions.ContainsKey(newTrade.Epic))
                    _positions.Add(newTrade.Epic, new Position(newTrade.Epic));
                _igStreamApiClient.BookTrade(newTrade, OnTradeBooked);
            }
        }

        public Position GetPosition(string epic)
        {
            if (!_positions.ContainsKey(epic))
                _positions.Add(epic, new Position(epic));
            return _positions[epic];
        }

        public TradingSet GetTradingSet(Model model)
        {
            string modelTypeStr = model.GetType().ToString();
            if (!_tradingSets.ContainsKey(modelTypeStr))
                _tradingSets[modelTypeStr] = model.CreateTradingSet(_igStreamApiClient);
            return _tradingSets[modelTypeStr];
        }

        void IHandyTableListener.OnRawUpdatesLost(int itemPos, string itemName, int lostUpdates)
        {
            foreach (var item in _positions)
                item.Value.OnRawUpdatesLost(lostUpdates);
            foreach (var tradingSet in _tradingSets)
            {
                foreach(var pos in tradingSet.Value.Positions)
                    pos.OnRawUpdatesLost(lostUpdates);
            }
        }

        void IHandyTableListener.OnSnapshotEnd(int itemPos, string itemName)
        {
            foreach (var item in _positions)
                item.Value.OnSnapshotEnd();
            foreach (var tradingSet in _tradingSets)
            {
                foreach (var pos in tradingSet.Value.Positions)
                    pos.OnSnapshotEnd();
            }
        }

        void IHandyTableListener.OnUnsubscr(int itemPos, string itemName)
        {
            foreach (var item in _positions)
                item.Value.OnUnsubscr();
            foreach (var tradingSet in _tradingSets)
            {
                foreach (var pos in tradingSet.Value.Positions)
                    pos.OnUnsubscr();
            }
        }

        void IHandyTableListener.OnUnsubscrAll()
        {
            foreach (var item in _positions)
                item.Value.OnUnsubscrAll();
            foreach (var tradingSet in _tradingSets)
            {
                foreach (var pos in tradingSet.Value.Positions)
                    pos.OnUnsubscrAll();
            }
        }

        void IHandyTableListener.OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            foreach (var item in _positions)
                item.Value.OnUpdate(update);
            foreach (var tradingSet in _tradingSets)
            {
                foreach (var pos in tradingSet.Value.Positions)
                    pos.OnUpdate(update);
            }
        }
    }

    // A trading is a set of tradable assets whose positions are independent from the portfolio
    // it needs to be aggregated to the portfolio for it to able to close those positions
    // useful to hold multiple temporary positions of a same instrument while keeping the granularity of each position
    public abstract class TradingSet
    {
        protected List<Position> _positions = new List<Position>();
        protected decimal? _stopLoss = null;
        IAbstractStreamingClient _client = null;

        public List<Position> Positions { get { return _positions; } }
        public decimal StopLoss { get { return _stopLoss.Value; } }

        protected TradingSet(IAbstractStreamingClient client, List<Position> positions = null, decimal? stopLoss = null)
        {
            _client = client;
            if (positions != null)
                _positions.AddRange(positions);
            _stopLoss = stopLoss;
        }

        protected void BookTrade(Trade newTrade, int idxPlaceHolder)
        {
            if (newTrade == null)
                return;
            if (Config.TradingOpen(newTrade.TradingTime))
            {
                newTrade.PlaceHolder = idxPlaceHolder;
                _client.BookTrade(newTrade, OnTradeBooked);
            }
        }
        
        void OnTradeBooked(Trade newTrade)
        {
            Portfolio.Instance.Trades.Add(newTrade);
            _positions[newTrade.PlaceHolder].Trade = newTrade;
            newTrade.Publish();
        }
    }
}
