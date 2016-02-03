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

        public delegate void TradeBookedEvent(Trade newTrade);

        public void Subscribe()
        {
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

        public void ClosePosition(Trade trade, DateTime closing_time, TradeBookedEvent onTradeBooked = null, TradeBookedEvent onBookingFailed = null)
        {
            if (trade == null)
                return;
            if (onTradeBooked == null)
                onTradeBooked = new TradeBookedEvent(OnTradeBooked);
            if (onBookingFailed == null)
                onBookingFailed = new TradeBookedEvent(OnBookingFailed); 
            _igStreamApiClient.ClosePosition(new Trade(trade, true, closing_time), closing_time, onTradeBooked, onBookingFailed);
        }
                
        public void CloseAllPositions(DateTime time, string stockid = "")
        {
            var addms = 1;
            foreach (var position in Positions)
            {
                if (stockid == "" || stockid == position.Value.Epic)
                    closePosition(position.Value, time.AddMilliseconds(addms++), new TradeBookedEvent(OnTradeBooked), new TradeBookedEvent(OnBookingFailed));
            }
            foreach (var tradingSet in _tradingSets)
                CloseTradingSet(tradingSet.Value, time);
        }

        public void CloseTradingSet(TradingSet set, DateTime time, decimal stockValue = 0m)
        {
            var idxPos = 0;
            var addms = 1;
            foreach (var pos in set.Positions)
                closePosition(pos, time.AddMilliseconds(addms++), new TradeBookedEvent(set.OnTradeBooked), new TradeBookedEvent(set.OnBookingFailed), idxPos++, stockValue);
        }

        void closePosition(Position pos, DateTime time, TradeBookedEvent onTradeBooked, TradeBookedEvent onBookingFailed, int idxPlaceHolder = 0, decimal stockValue = 0m)
        {
            if (pos.Quantity != 0)
            {
                if (pos.Closed)
                    Log.Instance.WriteEntry(string.Format("A position has not been closed successfully, Epic: {0}, Size: {1}, Value: {2}", pos.Epic, pos.Quantity, pos.AssetValue), System.Diagnostics.EventLogEntryType.Error);
                _igStreamApiClient.ClosePosition(new Trade(time, pos.Epic, pos.Quantity > 0 ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY, Math.Abs(pos.Quantity), stockValue, idxPlaceHolder), time, onTradeBooked, onBookingFailed);
                Log.Instance.WriteEntry(string.Format("Forcefully closed a position, Epic: {0}, Size: {1}, Value: {2}", pos.Epic, pos.Quantity, pos.AssetValue), System.Diagnostics.EventLogEntryType.Warning);
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
        
        protected virtual void OnTradeBooked(Trade newTrade)
        {
            _trades.Add(newTrade);
            GetPosition(newTrade.Epic).IncomingTrade = newTrade;
        }

        protected virtual void OnBookingFailed(Trade newTrade)
        {
            newTrade.Direction = SIGNAL_CODE.FAILED;
            GetPosition(newTrade.Epic).IncomingTrade = null;
            newTrade.Id = newTrade.Reference;
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
                _igStreamApiClient.BookTrade(newTrade, OnTradeBooked, OnBookingFailed);
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
                {
                    if (pos.OnUpdate(update))
                        break;
                }
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
        protected decimal? _objective = null;
        protected Signal _signal = null;
        protected bool _ready = false;
        IAbstractStreamingClient _client = null;        
        
        public List<Position> Positions { get { return _positions; } }
        public decimal StopLoss { get { return _stopLoss.Value; } }
        public decimal Objective { get { return _objective.Value; } }

        protected TradingSet(IAbstractStreamingClient client, List<Position> positions = null, decimal? stopLoss = null, decimal? objective = null)
        {
            _client = client;
            if (positions != null)
                _positions.AddRange(positions);
            _stopLoss = stopLoss;
            _objective = objective;
        }

        protected void BookTrade(Trade newTrade)
        {
            if (newTrade == null || _signal == null)
                return;
            if (Config.TradingOpen(newTrade.TradingTime))
                _client.BookTrade(newTrade, OnTradeBooked, OnBookingFailed);
        }

        public void ClosePosition(Trade trade, DateTime closing_time, decimal closing_price)
        {
            trade.Price = closing_price;
            _client.ClosePosition(trade, closing_time, OnTradeBooked, OnBookingFailed);            
        }

        public void OnTradeBooked(Trade newTrade)
        {
            _positions[newTrade.PlaceHolder].IncomingTrade = newTrade;
            publishSignal(newTrade);
        }

        public void OnBookingFailed(Trade newTrade)
        {
            _ready = false;
            newTrade.Direction = SIGNAL_CODE.FAILED;            
            _positions[newTrade.PlaceHolder].IncomingTrade = null;
            newTrade.Id = newTrade.Reference;
            newTrade.Publish();
        }

        void publishSignal(Trade newTrade)
        {
            Portfolio.Instance.Trades.Add(newTrade);
            _signal.Trade = newTrade;
            PublisherConnection.Instance.Insert(newTrade.TradingTime, _signal, newTrade.Direction, newTrade.Price);
        }
    }
}
