using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Model
    {
        protected DateTime _closingTime = DateTime.MaxValue;
        protected List<string> _tradingSignals = null;
        protected int _amount = 0;
        protected Portfolio _ptf = null;
        protected List<MarketData> _mktData = new List<MarketData>();
        protected List<Signal> _mktSignals = new List<Signal>();
        protected List<MarketData> _mktIndices = new List<MarketData>();
        protected List<Indicator> _mktIndicators = new List<Indicator>();
        protected List<ILevelPublisher> _mktEODIndicators = new List<ILevelPublisher>();
        protected bool _replayPopup = false;

        public Portfolio PTF { get { return _ptf; } }
        
        public Model()
        {
            _ptf = Portfolio.Instance;
        }

        public virtual void Init()
        {
            if (Config.Settings.ContainsKey("TRADING_CLOSING_TIME"))
                _closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            if (Config.Settings.ContainsKey("TRADING_SIGNAL"))
                _tradingSignals = Config.Settings["TRADING_SIGNAL"].Split(',').ToList();
            if (Config.Settings.ContainsKey("REPLAY_POPUP"))
                _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
            _amount = Config.MarketSelectorEnabled ? 0 : int.Parse(Config.Settings["TRADING_LIMIT_PER_BP"]);
            if (_mktData[0].Name == "SIL")
                _amount *= 6;
            else if (_mktData[0].Name == "FTSE")
                _amount = (int)(_amount * 1.5);
        }

        const int MAX_BOOKING_ATTEMPTS = 50;
        int _bookingRemainingAttempts = MAX_BOOKING_ATTEMPTS;

        protected bool FilterSignal(Signal signal, DateTime time)
        {            
            if (_tradingSignals != null)
            {
                if (_tradingSignals.Contains(signal.Id))
                {
                    if (_ptf.IsWaiting(signal.TradingAsset.Id))
                    {
                        if (--_bookingRemainingAttempts == 0)
                        {
                            if (Portfolio.Instance.ShutDownFunc != null)
                            {
                                Log.Instance.WriteEntry("Terminating...", System.Diagnostics.EventLogEntryType.Error);
                                Portfolio.Instance.ShutDownFunc();
                            }
                        }
                        Log.Instance.WriteEntry(string.Format("Cannot book a new trade as a deal is already pending, Epic: {0}. Waiting for position to be updated", signal.TradingAsset.Id), System.Diagnostics.EventLogEntryType.Warning);
                        return false;
                    }
                    _bookingRemainingAttempts = MAX_BOOKING_ATTEMPTS;
                    var pos = _ptf.GetPosition(signal.TradingAsset.Id);
                    if (pos != null)
                    {
                        if (pos.ManualOverride)
                        {
                            Log.Instance.WriteEntry("Applying a signal manual override on: " + signal.Id, System.Diagnostics.EventLogEntryType.Information);
                            pos.ManualOverride = false;
                            signal.Enabled = false;
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        protected virtual bool OnBuy(Signal signal, DateTime time, Price stockValue)
        {
            if (FilterSignal(signal, time))
            {
                var pos = _ptf.GetPosition(signal.TradingAsset.Id);
                if (pos == null)
                {
                    Reset(time, stockValue.Mid(), true);
                    return false;
                }
                if (pos.Quantity > 0)
                {
                    Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Some trades are still open. last trade: " + signal.Trade.Id + " " + stockValue.Bid + ". Closing all positions...", EventLogEntryType.Error);
                    _ptf.CloseAllPositions(time, signal.TradingAsset.Id, stockValue.Bid);
                    return false;
                }
                signal.Trade = new Trade(time, signal.TradingAsset.Id, SIGNAL_CODE.BUY, _amount, stockValue.Offer, 0);
                return Buy(signal, time, stockValue);
            }
            return false;
        }

        protected virtual bool OnSell(Signal signal, DateTime time, Price stockValue)
        {
            if (FilterSignal(signal, time))
            {
                var pos = _ptf.GetPosition(signal.TradingAsset.Id);
                if (pos == null)
                {
                    Reset(time, stockValue.Mid(), true);
                    return false;
                }
                if (pos.Quantity < 0)
                {
                    Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Some trades are still open. last trade: " + signal.Trade.Id + " " + stockValue.Bid + ". Closing all positions...", EventLogEntryType.Error);
                    _ptf.CloseAllPositions(time, signal.TradingAsset.Id, stockValue.Offer);
                    return false;
                }
                signal.Trade = new Trade(time, signal.TradingAsset.Id, SIGNAL_CODE.SELL, _amount, stockValue.Bid, 0);
                return Sell(signal, time, stockValue);
            }
            return false;
        }

        protected abstract bool Buy(Signal signal, DateTime time, Price stockValue);
        protected abstract bool Sell(Signal signal, DateTime time, Price stockValue);
        protected abstract void Reset(DateTime cancelTime, decimal stockValue, bool openPosition);

        protected virtual void OnUpdateMktData(MarketData mktData, DateTime updateTime, Price value)
        {
        }
        protected virtual void OnUpdateIndicator(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        public void StartSignals(bool startListening = true, Trader.shutdown communicatorShutdown = null)
        {
            _ptf.ShutDownFunc = communicatorShutdown;
            Init();
            
            // get the level indicators for the day (low, high, close)
            foreach (MarketData mktData in _mktData)
                mktData.GetMarketLevels();
            List<MarketData> eodLevelMktData = (from mktdata in _mktIndices where mktdata.HasEODLevels select mktdata).ToList();
            foreach (MarketData mktData in eodLevelMktData)
                mktData.GetMarketLevels();  
            // subscribe indicators and signals to market data feed
            foreach (MarketData idx in _mktIndices)
                idx.Subscribe(OnUpdateMktData, null);
            foreach (Indicator ind in _mktIndicators)
                ind.Subscribe(OnUpdateIndicator, null);
            foreach (Signal sig in _mktSignals)
                sig.Subscribe(OnBuy, OnSell);
            if (startListening)
                MarketDataConnection.Instance.StartListening();
        }

        public void StopSignals(bool stopListening = true)
        {
            try
            {
                //Thread.Sleep(1000); 
                Log.Instance.WriteEntry("Publishing indicator levels...", EventLogEntryType.Information);
                foreach (var indicator in _mktEODIndicators)
                    indicator.Publish(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
                //MarketDataConnection.Instance.PublishMarketLevels(_mktData);
                //List<MarketData> eodLevelMktData = (from mktdata in _mktIndices where mktdata.HasEODLevels select mktdata).ToList();
                //MarketDataConnection.Instance.PublishMarketLevels(eodLevelMktData);
            }
            finally
            {
                if (stopListening)
                    MarketDataConnection.Instance.StopListening();                    
                foreach (Signal sig in (from s in _mktSignals select s).Reverse())
                    sig.Unsubscribe();
                foreach (Indicator ind in (from i in _mktIndicators select i).Reverse())
                {
                    ind.Unsubscribe(OnUpdateIndicator, null);
                    ind.Clear();
                }
                foreach (MarketData idx in (from i in _mktIndices select i).Reverse())
                {
                    idx.Unsubscribe(OnUpdateMktData, null);
                    idx.Clear();
                }
                foreach (MarketData stock in (from s in _mktData select s).Reverse())
                    stock.Clear();
            }
        }   

        public virtual void ProcessError(string message, string expected = "")
        {            
        }

        public virtual TradingSet CreateTradingSet(IAbstractStreamingClient client)
        {
            return null;
        }
    }

    public class ModelALaCon : Model
    {
        MarketData _fx;

        public ModelALaCon(MarketData fx)
        {
            _fx = fx;
        }

        public override void Init()
        {
            base.Init();
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(_fx);
            _mktIndices.Add(_fx);
            _mktData = mktData;
            _mktSignals.Add(new SignalALaCon(_fx));
        }

        protected override void Reset(DateTime cancelTime, decimal stockValue, bool openPosition)
        {
            Log.Instance.WriteEntry("Aaaaaarrr", EventLogEntryType.Information);
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (!_ptf.BookTrade(signal.Trade))
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.TradingAsset.Id + " " + stockValue.Bid, EventLogEntryType.Information);
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (!_ptf.BookTrade(signal.Trade))
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.TradingAsset.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            return true;
        }
    }
}
