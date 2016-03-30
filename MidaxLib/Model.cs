﻿using System;
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
        }

        public virtual void Init()
        {
            _closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            if (Config.Settings.ContainsKey("TRADING_SIGNAL"))
                _tradingSignal = Config.Settings["TRADING_SIGNAL"];
            if (Config.Settings.ContainsKey("REPLAY_POPUP"))
                _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
            _amount = Config.MarketSelectorEnabled ? 0 : int.Parse(Config.Settings["TRADING_LIMIT_PER_BP"]);
            _ptf = Portfolio.Instance;
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

    public class ModelMacD : Model
    {
        protected MarketData _daxIndex = null;
        protected SignalMacD _macD_low = null;
        protected SignalMacD _macD_high = null;
        protected SIGNAL_CODE _trendAssumption = SIGNAL_CODE.UNKNOWN;
        int _tradeSize = 0;
        int _lowPeriod = 0;
        int _midPeriod = 0;
        int _highPeriod = 0;

        public MarketData Index { get { return _daxIndex; } }
        public SignalMacD SignalLow { get { return _macD_low; } }
        public SignalMacD SignalHigh { get { return _macD_high; } }

        public int LowPeriod { get { return _lowPeriod; } }
        public int MidPeriod { get { return _midPeriod; } }
        public int HighPeriod { get { return _highPeriod; } }

        public ModelMacD(MarketData daxIndex, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60)
        {
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trendAssumption = Config.Settings["ASSUMPTION_TREND"] == "BULL" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL;
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);         
            _mktData = mktData;
            _daxIndex = daxIndex;
            _lowPeriod = lowPeriod;
            _midPeriod = midPeriod;
            _highPeriod = highPeriod;
        }

        public override void Init()
        {
            base.Init();
            _macD_low = new SignalMacD(_daxIndex, _lowPeriod, _midPeriod);
            _macD_high = new SignalMacD(_daxIndex, _midPeriod, _highPeriod, _macD_low.IndicatorHigh);
            _mktSignals.Add(_macD_low);
            _mktSignals.Add(_macD_high);
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity < 0)
            {
                if (time >= _closingTime)
                {
                    signal.Trade.Price = stockValue.Offer;
                    _ptf.ClosePosition(signal.Trade, time, null, null, signal);
                    string closeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + closeRef + " Signal close " + signal.Id + ": BUY " + signal.MarketData.Id + " " + stockValue.Bid, EventLogEntryType.Information);
                    return false;
                }
                else
                {
                    if (_trendAssumption != SIGNAL_CODE.SELL)
                        signal.Trade.Size = _tradeSize * 2;
                    _ptf.BookTrade(signal.Trade);
                }
            }
            else if (_trendAssumption != SIGNAL_CODE.SELL && _ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _tradeSize = signal.Trade.Size;
                    _ptf.BookTrade(signal.Trade);
                }
                else
                    return false;
            }
            else
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + stockValue.Bid, EventLogEntryType.Information);
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity > 0)
            {
                if (time >= _closingTime)
                {
                    signal.Trade.Price = stockValue.Bid;
                    _ptf.ClosePosition(signal.Trade, time, null, null, signal);
                    string closeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + closeRef + " Signal close " + signal.Id + ": SELL " + signal.MarketData.Id + " " + stockValue.Offer, EventLogEntryType.Information);
                    return false;
                }
                else
                {
                    if (_trendAssumption != SIGNAL_CODE.BUY)
                        signal.Trade.Size = _tradeSize * 2;
                    _ptf.BookTrade(signal.Trade);
                }
            }
            else if (_trendAssumption != SIGNAL_CODE.BUY && _ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _tradeSize = signal.Trade.Size;
                    _ptf.BookTrade(signal.Trade);
                }
                else
                    return false;
            }
            else
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            return true;
        }        
    }
}
