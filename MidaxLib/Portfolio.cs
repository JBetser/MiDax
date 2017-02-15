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
        IGPublicPcl.IgRestApiClient _igRestApiClient = null;
        SubscribedTableKey _tradeSubscriptionStk = null;
        static Dictionary<IAbstractStreamingClient, Portfolio> _instance = null;
        Dictionary<string, TradingSet> _tradingSets = new Dictionary<string, TradingSet>();

        public List<Trade> Trades { get { return _trades; } }
        public Dictionary<string, Position> Positions { get { return _positions; } }

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

        public delegate void TradeBookedEvent(Trade newTrade);

        public async void Subscribe()
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
                        var trade = new Trade(DateTime.Parse(pos.market.updateTime), pos.market.epic,
                            pos.position.direction == "BUY" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL, (int)pos.position.size.Value,
                            pos.position.direction == "BUY" ? pos.market.offer.Value : pos.market.bid.Value);
                        trade.Reference = "RECOVER_" + pos.position.dealId;
                        trade.ConfirmationTime = trade.TradingTime;
                        ReplayPositionUpdateInfo updateInfo = new ReplayPositionUpdateInfo(DateTime.Parse(pos.market.updateTime), pos.market.epic,
                            pos.position.dealId, trade.Reference, "OPEN", "ACCEPTED", (int)pos.position.size.Value,
                            pos.position.direction == "BUY" ? pos.market.offer.Value : pos.market.bid.Value,
                            pos.position.direction == "BUY" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL);
                        var ptfPos = GetPosition(pos.market.epic);
                        if (ptfPos != null)
                        {
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

        public void ClosePosition(Trade trade, DateTime closing_time, TradeBookedEvent onTradeBooked = null, TradeBookedEvent onBookingFailed = null, Signal signal = null)
        {
            if (trade == null)
                return;
            if (onTradeBooked == null)
                onTradeBooked = new TradeBookedEvent(OnTradeBooked);
            if (onBookingFailed == null)
                onBookingFailed = new TradeBookedEvent(OnBookingFailed);
            closePosition(new Trade(trade, true, closing_time), closing_time, onTradeBooked, onBookingFailed, 0, signal);
        }

        public void ClosePosition(Position pos, DateTime closing_time, TradeBookedEvent onTradeBooked = null, TradeBookedEvent onBookingFailed = null, decimal stockValue = 0m, Signal signal = null)
        {
            if (onTradeBooked == null)
                onTradeBooked = new TradeBookedEvent(OnTradeBooked);
            if (onBookingFailed == null)
                onBookingFailed = new TradeBookedEvent(OnBookingFailed);
            closePosition(pos, closing_time, onTradeBooked, onBookingFailed, 0, stockValue, signal);
        }

        public void CloseAllPositions(DateTime time, string mktdataid = "", decimal stockValue = 0m, Signal signal = null)
        {
            var addms = 1;
            foreach (var position in Positions)
            {
                if (mktdataid == "" || mktdataid == position.Value.Epic)
                    closePosition(position.Value, time.AddMilliseconds(addms++), new TradeBookedEvent(OnTradeBooked), new TradeBookedEvent(OnBookingFailed), 0, stockValue, signal);
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

        public void closePosition(Position pos, DateTime time, TradeBookedEvent onTradeBooked, TradeBookedEvent onBookingFailed, int idxPlaceHolder = 0, decimal stockValue = 0m, Signal signal = null)
        {
            if (pos.Quantity != 0)
            {
                var positionClose = new Trade(time, pos.Epic, pos.Quantity > 0 ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY, Math.Abs(pos.Quantity), stockValue, idxPlaceHolder);
                closePosition(positionClose, time, onTradeBooked, onBookingFailed, idxPlaceHolder, signal);
            }
        }

        void closePosition(Trade trade, DateTime time, TradeBookedEvent onTradeBooked, TradeBookedEvent onBookingFailed, int idxPlaceHolder = 0, Signal signal = null)
        {
            foreach (var pos in _positions.Values){
                if (pos.AwaitingTrade)
                {
                    Log.Instance.WriteEntry(string.Format("Cannot close a position as deal is already pending, Epic: {0}, Size: {1}, Value: {2}. Waiting for position {3} to be updated", trade.Epic, trade.Size, trade.Price, pos.Epic), System.Diagnostics.EventLogEntryType.Warning);
                    return;
                }
            }
            _igStreamApiClient.ClosePosition(trade, time, onTradeBooked, onBookingFailed);
            Log.Instance.WriteEntry(string.Format("Forcefully closed a position, Epic: {0}, Size: {1}, Value: {2}", trade.Epic, trade.Size, trade.Price), System.Diagnostics.EventLogEntryType.Warning);
            if (signal != null)
            {
                signal.Trade = trade;
                PublisherConnection.Instance.Insert(time, signal, trade.Direction, trade.Price);
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
            var pos = GetPosition(newTrade.Epic);
            if (pos != null)
            {
                _trades.Add(newTrade);
                pos.AddIncomingTrade(newTrade);
            }
        }

        protected virtual void OnBookingFailed(Trade newTrade)
        {
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

        public Position GetPosition(string epic)
        {
            foreach (var pos in _positions.Values)
            {
                if (pos.Epic == epic)
                {
                    if (pos.AwaitingTrade)
                    {
                        Log.Instance.WriteEntry(string.Format("Cannot book a new trade as a deal is already pending, Epic: {0}. Waiting for position to be updated", epic), System.Diagnostics.EventLogEntryType.Warning);
                        return null;
                    }
                }
            }
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
            PublisherConnection.Instance.Insert(newTrade.TradingTime, _signal, newTrade.Direction, newTrade.Price);
        }
    }
}
