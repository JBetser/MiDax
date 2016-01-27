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
            _closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            if (Config.Settings.ContainsKey("TRADING_SIGNAL"))
                _tradingSignal = Config.Settings["TRADING_SIGNAL"];
            if (Config.Settings.ContainsKey("REPLAY_POPUP"))
                _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
            _amount = Config.MarketSelectorEnabled ? 0 : int.Parse(Config.Settings["TRADING_LIMIT_PER_BP"]);
            _ptf = Portfolio.Instance;
        }

        protected bool OnBuy(Signal signal, DateTime time, Price value)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                    return Buy(signal, time, signal.MarketData.TimeSeries[time].Value.Value);
            }
            return true;
        }

        protected bool OnSell(Signal signal, DateTime time, Price stockValue)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                {
                    if (!_ptf.GetPosition(signal.MarketData.Id).Closed)
                    {
                        if (_ptf.GetPosition(signal.MarketData.Id).Quantity >= 0)
                        {
                            Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Some trades are still open. last trade: " + signal.Trade.Id + " " + stockValue.Bid + ". Closing all positions...", EventLogEntryType.Error);
                            Portfolio.Instance.CloseAllPositions(time, signal.MarketData.Id);
                        }
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
            // get the level indicators for the day (low, high, close)
            foreach (MarketData mktData in _mktData)
                mktData.GetMarketLevels();
            List<MarketData> eodLevelMktData = (from mktdata in _mktIndices where mktdata.HasEODLevels select mktdata).ToList();
            foreach (MarketData mktData in eodLevelMktData)
                mktData.GetMarketLevels();  
            // subscribe indicators and signals to market data feed
            foreach (MarketData idx in _mktIndices)
                idx.Subscribe(OnUpdateMktData);
            foreach (Indicator ind in _mktIndicators)
                ind.Subscribe(OnUpdateIndicator);
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
                MarketDataConnection.Instance.PublishMarketLevels(_mktData);
                List<MarketData> eodLevelMktData = (from mktdata in _mktIndices where mktdata.HasEODLevels select mktdata).ToList();
                MarketDataConnection.Instance.PublishMarketLevels(eodLevelMktData);
            }
            finally
            {
                if (stopListening)
                    MarketDataConnection.Instance.StopListening();                    
                foreach (Signal sig in _mktSignals)
                {
                    sig.Unsubscribe();
                    sig.Clear();
                }
                foreach (Indicator ind in _mktIndicators)
                {
                    ind.Unsubscribe(OnUpdateIndicator);
                    ind.Clear();
                }
                foreach (MarketData idx in _mktIndices)
                {
                    idx.Unsubscribe(OnUpdateMktData);
                    idx.Clear();
                }
                foreach (MarketData stock in _mktData)
                    stock.Clear();
            }
        }        
        
        public void BookTrade(Trade trade)
        {
            _ptf.BookTrade(trade);
        }

        public virtual void ProcessError(string message, string expected = "")
        {            
        }

        public virtual TradingSet CreateTradingSet(IAbstractStreamingClient client)
        {
            return null;
        }
    }

    public class ModelMacD : Model
    {
        protected MarketData _daxIndex = null;
        protected SignalMacD _macD_low = null;
        protected SignalMacD _macD_high = null;

        public MarketData Index { get { return _daxIndex; } }
        public SignalMacD SignalLow { get { return _macD_low; } }
        public SignalMacD SignalHigh { get { return _macD_high; } }

        public ModelMacD(MarketData daxIndex, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);         
            _mktData = mktData;
            _daxIndex = daxIndex;
            _macD_low = new SignalMacD(_daxIndex, lowPeriod, midPeriod);
            _macD_high = new SignalMacD(_daxIndex, midPeriod, highPeriod, _macD_low.IndicatorHigh);
            _mktSignals.Add(_macD_low);
            _mktSignals.Add(_macD_high);
            _mktEODIndicators.Add(new IndicatorLevelMean(_daxIndex));
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity < 0)
            {
                signal.Trade.Price = stockValue.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            }
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + stockValue.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + stockValue.Bid, EventLogEntryType.Information);
                }
            }
            return true;
        }        
    }
}
