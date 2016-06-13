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
        protected DateTime _closingTime = DateTime.MinValue;
        protected string _tradingSignal = null;
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

        protected virtual void Init()
        {
            _closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            if (Config.Settings.ContainsKey("TRADING_SIGNAL"))
                _tradingSignal = Config.Settings["TRADING_SIGNAL"];
            if (Config.Settings.ContainsKey("REPLAY_POPUP"))
                _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
            _amount = Config.MarketSelectorEnabled ? 0 : int.Parse(Config.Settings["TRADING_LIMIT_PER_BP"]);            
        }

        protected virtual bool OnBuy(Signal signal, DateTime time, Price stockValue)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                {
                    if (_ptf.GetPosition(signal.MarketData.Id).Quantity > 0)
                    {
                        Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Some trades are still open. last trade: " + signal.Trade.Id + " " + stockValue.Bid + ". Closing all positions...", EventLogEntryType.Error);
                        Portfolio.Instance.CloseAllPositions(time, signal.MarketData.Id, stockValue.Bid, signal);
                        return false;
                    }
                    signal.Trade = new Trade(time, signal.MarketData.Id, SIGNAL_CODE.BUY, _amount, stockValue.Offer);
                    return Buy(signal, time, stockValue);
                }
            }
            return true;
        }

        protected virtual bool OnSell(Signal signal, DateTime time, Price stockValue)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                {
                    if (_ptf.GetPosition(signal.MarketData.Id).Quantity < 0)
                    {
                        Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Some trades are still open. last trade: " + signal.Trade.Id + " " + stockValue.Bid + ". Closing all positions...", EventLogEntryType.Error);
                        Portfolio.Instance.CloseAllPositions(time, signal.MarketData.Id, stockValue.Offer, signal);
                        return false;
                    }
                    signal.Trade = new Trade(time, signal.MarketData.Id, SIGNAL_CODE.SELL, _amount, stockValue.Bid);
                    return Sell(signal, time, stockValue);
                }
            }
            return true;
        }

        protected abstract bool Buy(Signal signal, DateTime time, Price stockValue);
        protected abstract bool Sell(Signal signal, DateTime time, Price stockValue);

        protected virtual void OnUpdateMktData(MarketData mktData, DateTime updateTime, Price value)
        {
        }
        protected virtual void OnUpdateIndicator(MarketData mktData, DateTime updateTime, Price value)
        {
        }
        
        public void StartSignals(bool startListening = true)
        {
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
}
