using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class Portfolio : IHandyTableListener
    {
        ConcurrentDictionary<string, Position> _positions = new ConcurrentDictionary<string, Position>();
        List<Trade> _trades = new List<Trade>();
        IAbstractStreamingClient _igStreamApiClient = null;
        IGPublicPcl.IgRestApiClient _igRestApiClient = null;
        SubscribedTableKey _tradeSubscriptionStk = null;
        static Dictionary<IAbstractStreamingClient, Portfolio> _instance = null;
        Dictionary<string, TradingSet> _tradingSets = new Dictionary<string, TradingSet>();
        Trader.shutdown _onShutdown = null;

        public List<Trade> Trades { get { return _trades; } }
        public IDictionary<string, Position> Positions { get { return _positions; } }
        public Trader.shutdown ShutDownFunc { get { return _onShutdown; } set { _onShutdown = value; } }

        Portfolio(IAbstractStreamingClient client, IGPublicPcl.IgRestApiClient restApiClient)
        {
            _igStreamApiClient = client;
            _igRestApiClient = restApiClient;
        }

        public static Portfolio Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Dictionary<IAbstractStreamingClient, Portfolio>();
                if (!_instance.ContainsKey(MarketDataConnection.Instance.StreamClient))
                    _instance[MarketDataConnection.Instance.StreamClient] = new Portfolio(MarketDataConnection.Instance.StreamClient, MarketDataConnection.Instance.RestClient);
                return _instance[MarketDataConnection.Instance.StreamClient];
            }
        }

        /// <summary>
        /// This is only in case we lose synchronization with the book
        /// </summary>
        public void ReSubscribe()
        {
            _positions.Clear();
            _tradingSets.Clear();
            _trades.Clear();
            _positions.Clear();
            var task = Subscribe();
            task.Wait();
        }

        public delegate void TradeBookedEvent(Trade newTrade);

        public async Task Subscribe()
        {
            try
            {
                if (_igStreamApiClient != null)
                {
                    _tradeSubscriptionStk = _igStreamApiClient.SubscribeToPositions(this);
                    Log.Instance.WriteEntry("TradeSubscription : Subscribe");
                    var response = await _igRestApiClient.getOTCOpenPositionsV2();
                    foreach (var pos in response.Response.positions)
                    {                        
                        var ptfPos = GetPosition(pos.market.epic);
                        if (ptfPos != null)
                        {
                            var trade = new Trade(DateTime.Parse(pos.market.updateTime), pos.market.epic,
                            pos.position.direction == "BUY" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL, (int)pos.position.size.Value,
                            pos.position.direction == "BUY" ? pos.market.offer.Value : pos.market.bid.Value);
                            trade.Id = pos.position.dealId;
                            trade.Reference = "RECOVER_" + pos.position.dealId;
                            trade.ConfirmationTime = trade.TradingTime;
                            ReplayPositionUpdateInfo updateInfo = new ReplayPositionUpdateInfo(DateTime.Parse(pos.market.updateTime), pos.market.epic,
                                pos.position.dealId, trade.Reference, "OPEN", "ACCEPTED", (int)pos.position.size.Value,
                                pos.position.direction == "BUY" ? pos.market.offer.Value : pos.market.bid.Value,
                                pos.position.direction == "BUY" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL);
                            _trades.Add(trade);
                            ptfPos.AddIncomingTrade(trade);
                            ptfPos.OnUpdate(updateInfo);
                        }
                        else 
                        {
                            Log.Instance.WriteEntry("Trade subscription error: mktdata: " + pos.market.epic + ", direction: " + pos.position.direction + ", size: " + pos.position.size.Value +
                                ", dealid:" + pos.position.dealId, System.Diagnostics.EventLogEntryType.Error);
                        }
                    }
                                
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
            closePosition(new Trade(trade, true, closing_time), closing_time, onTradeBooked, onBookingFailed);
        }

        public void ClosePosition(Position pos, DateTime closing_time, TradeBookedEvent onTradeBooked = null, TradeBookedEvent onBookingFailed = null, decimal stockValue = 0m)
        {
            if (onTradeBooked == null)
                onTradeBooked = new TradeBookedEvent(OnTradeBooked);
            if (onBookingFailed == null)
                onBookingFailed = new TradeBookedEvent(OnBookingFailed);
            closePosition(pos, closing_time, onTradeBooked, onBookingFailed, 0, stockValue);
        }

        public void CloseAllPositions(DateTime time, string mktdataid = "", decimal stockValue = 0m)
        {
            var addms = 1;
            foreach (var position in Positions)
            {
                if (mktdataid == "" || mktdataid == position.Value.Epic)
                    closePosition(position.Value, time.AddMilliseconds(addms++), new TradeBookedEvent(OnTradeBooked), new TradeBookedEvent(OnBookingFailed), 0, stockValue);
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

        public void closePosition(Position pos, DateTime time, TradeBookedEvent onTradeBooked, TradeBookedEvent onBookingFailed, int idxPlaceHolder = 0, decimal stockValue = 0m)
        {
            if (pos.Quantity != 0)
            {
                var positionClose = new Trade(time, pos.Epic, pos.Quantity > 0 ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY, Math.Abs(pos.Quantity), stockValue, idxPlaceHolder);
                closePosition(positionClose, time, onTradeBooked, onBookingFailed);
            }
        }

        void closePosition(Trade trade, DateTime time, TradeBookedEvent onTradeBooked, TradeBookedEvent onBookingFailed)
        {
            _igStreamApiClient.ClosePosition(trade, time, onTradeBooked, onBookingFailed);
            Log.Instance.WriteEntry(string.Format("Forcefully closed a position, Epic: {0}, Size: {1}, Value: {2}", trade.Epic, trade.Size, trade.Price), System.Diagnostics.EventLogEntryType.Warning);
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
            var pos = GetPosition(newTrade.Epic);
            if (pos != null)
            {
                _trades.Add(newTrade);
                pos.AddIncomingTrade(newTrade);
            }
        }

        protected virtual void OnBookingFailed(Trade newTrade)
        {
            _trades.Remove(newTrade);
            var pos = GetPosition(newTrade.Epic);
            if (pos != null)
                pos.RemoveIncomingTrade(newTrade);
            newTrade.Direction = SIGNAL_CODE.FAILED;
            newTrade.Id = newTrade.Reference;
            newTrade.Publish();
        }
        
        public bool BookTrade(Trade newTrade)
        {
            if (newTrade == null)
                return false;
            if (Config.TradingOpen(newTrade.TradingTime))
            {
                if (GetPosition(newTrade.Epic) != null)
                {
                    _igStreamApiClient.BookTrade(newTrade, OnTradeBooked, OnBookingFailed);
                    return true;
                }
            }
            return false;
        }

        public bool IsWaiting(string epic)
        {
            foreach (var pos in _positions.Values)
            {
                if (pos.Epic == epic)
                {
                    if (pos.AwaitingTrade)
                        return true;
                }
            }
            return false;
        }

        public bool IsWaiting()
        {
            foreach (var pos in _positions.Values)
            {
                if (pos.AwaitingTrade)
                    return true;
            }
            return false;
        }

        public Position GetPosition(string epic)
        {
            lock (_positions)
            {
                if (!_positions.ContainsKey(epic))
                    _positions[epic] = new Position(epic);
                return _positions[epic];
            }
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

        DateTime? _lastUpdate = null;

        void Synchronize()
        {
            MarketDataConnection.Instance.SetListeningState(false);
            ReSubscribe();
            MarketDataConnection.Instance.SetListeningState(true);
            Log.Instance.WriteEntry("Positions synchronized", System.Diagnostics.EventLogEntryType.Information);                        
        }

        void IHandyTableListener.OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            if (update != null)
            {
                if (update.NumFields != 0)
                {
                    if ((update.ToString().Replace(" ", "") == "[(null)]") || 
                        (update.ToString().Replace(" ", "") == "[null]"))
                    {
                        if (_lastUpdate.HasValue)
                        {
                            if ((DateTime.Now - _lastUpdate.Value).TotalMinutes < 1)
                            {
                                Log.Instance.WriteEntry("Ignored null update", System.Diagnostics.EventLogEntryType.Warning);
                                return;
                            }
                            _lastUpdate = DateTime.Now;
                        }
                        else
                        {
                            _lastUpdate = DateTime.Now;
                            return;
                        }
                        Log.Instance.WriteEntry("Null update, synchronizing positions...", System.Diagnostics.EventLogEntryType.Warning);
                        Synchronize();
                        return;
                    }
                    bool updateProcessed = false;
                    foreach (var item in _positions)
                    {
                        if (item.Value.OnUpdate(update))
                        {
                            updateProcessed = true;
                            break;
                        }
                    }                    
                    foreach (var tradingSet in _tradingSets)
                    {
                        foreach (var pos in tradingSet.Value.Positions)
                        {
                            if (pos.OnUpdate(update))
                            {
                                updateProcessed = true;
                                break;
                            }
                        }
                    }
                    if (!updateProcessed)
                    {
                        Log.Instance.WriteEntry("Unexpected update, synchronizing positions... update: " + update.ToString(), System.Diagnostics.EventLogEntryType.Warning);
                        Synchronize();
                    }
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
            _positions[newTrade.PlaceHolder].AddIncomingTrade(newTrade);
            publishSignal(newTrade);
        }

        public void OnBookingFailed(Trade newTrade)
        {
            _ready = false;
            newTrade.Direction = SIGNAL_CODE.FAILED;            
            newTrade.Id = newTrade.Reference;
            newTrade.Publish();
        }

        void publishSignal(Trade newTrade)
        {
            Portfolio.Instance.Trades.Add(newTrade);
            _signal.Trade = newTrade;
        }
    }
}
