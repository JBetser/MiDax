using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelMacDCascade : Model
    {
        protected MarketData _daxIndex = null;

        public ModelMacDCascade(ModelMacD macD)
        {
            _daxIndex = macD.Index;
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 1.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 2.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
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

    public class ModelMole : Model
    {
        protected MarketData _daxIndex = null;
        protected SignalMacD _signal = null;
        protected SignalMacD _signalLong = null;
        protected TradingSetMole _tradingSet = null;
        IndicatorWMA _wmaMid = null;
        MarketLevels? _mktLevels;

        public ModelMole(ModelMacD macD)
        {
            _daxIndex = macD.Index;
            _wmaMid = macD.SignalLow.IndicatorHigh;
            _mktLevels = _daxIndex.Levels;
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(_daxIndex);
            _mktData = mktData;
            _signal = new SignalMole(_daxIndex, macD.SignalLow.IndicatorLow.Period / 60, macD.SignalLow.IndicatorHigh.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, macD.SignalLow.IndicatorLow, macD.SignalLow.IndicatorHigh);
            _mktSignals.Add(_signal);
            _signalLong = new SignalMole(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh);
            _signalLong.Subscribe(new Signal.Tick(LongBuy), new Signal.Tick(LongSell));
            _tradingSet = (TradingSetMole)Portfolio.Instance.GetTradingSet(this);
            _daxIndex.Subscribe(new MarketData.Tick(OnUpdateIndex), null);
            _wmaMid.Subscribe(new MarketData.Tick(OnUpdateWMA), null);            
            _tradingSet.Init(_daxIndex);
        }

        protected override bool OnBuy(Signal signal, DateTime time, Price value)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                    return Buy(signal, time, signal.MarketData.TimeSeries[time].Value.Value);
            }
            return false;
        }

        protected override bool OnSell(Signal signal, DateTime time, Price stockValue)
        {
            if (_tradingSignal != null)
            {
                if (signal.Id == _tradingSignal)
                {
                    signal.Trade = new Trade(time, signal.MarketData.Id, SIGNAL_CODE.SELL, _amount, stockValue.Bid);
                    return Sell(signal, time, stockValue);
                }
            }
            return false;
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            return false;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            _tradingSet.PlaceTrade(signal.Trade, stockValue.Bid);
            return false;
        }

        bool LongBuy(Signal signal, DateTime time, Price stockValue)
        {
            _tradingSet.Stop(time, stockValue.Offer);
            return false;
        }

        bool LongSell(Signal signal, DateTime time, Price stockValue)
        {
            _tradingSet.Start();
            return false;
        }

        protected virtual void OnUpdateIndex(MarketData mktData, DateTime updateTime, Price stockValue)
        {
            _tradingSet.UpdateIndex(updateTime, stockValue.Offer); 
        }

        protected void OnUpdateWMA(MarketData indicator, DateTime updateTime, Price value)
        {
            _tradingSet.SetReferenceLevel(value.Mid(), _signal, _mktLevels);
        }

        public override TradingSet CreateTradingSet(IAbstractStreamingClient client)
        {
            return new TradingSetMole(client);
        }
    }

    public class Interval
    {
        public readonly decimal Min;
        public readonly decimal Max;

        public Interval(decimal min, decimal max)
        {
            Min = min;
            Max = max;
        }

        public bool IsInside(decimal value)
        {
            return value >= Min && value < Max;
        }
    }

    public class TradingSetMole : TradingSet
    {
        decimal _referenceLevel = 0.0m;
        int _nbPlaceholders = 0;
        Dictionary<Interval, Position> _placeHolders = new Dictionary<Interval, Position>();

        public bool Ready { get { return _ready; } }

        public TradingSetMole(IAbstractStreamingClient client)
            : base(client)
        {
            _stopLoss = 40.0m;
            _objective = 9.0m;
            _nbPlaceholders = 8;
        }

        public void Init(MarketData index)
        {
            int nbplaceholders = _nbPlaceholders;
            while(nbplaceholders-- > 0)
                _positions.Add(new Position(index.Id));
            var idxPlaceHolder = 0;
            _placeHolders[new Interval(-15.0m, -10.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(-10.0m, -5.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(-5.0m, -2.5m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(-2.5m, 0.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(0.0m, 2.5m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(2.5m, 5.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(5.0m, 10.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(10.0m, 15.0m)] = _positions[idxPlaceHolder++];
            if (idxPlaceHolder != _nbPlaceholders)
                throw new ApplicationException("Mole init error: Inconsistent number of placeholders");
        }

        public void Start()
        {
            _ready = true;
        }

        public void Stop(DateTime time, decimal stockValue)
        {
            _ready = false;
            Portfolio.Instance.CloseTradingSet(this, time, stockValue);
        }

        public bool PlaceTrade(Trade trade, decimal price)
        {
            if (!_ready || !Config.TradingOpen(trade.TradingTime))
                return false;
            int idxPlaceHolder = -1;
            foreach (var placeHolder in _placeHolders)
            {
                idxPlaceHolder++;
                if (!placeHolder.Key.IsInside(price - _referenceLevel))
                    continue;
                if (!placeHolder.Value.Closed || placeHolder.Value.AwaitingTrade)
                    continue;
                trade.PlaceHolder = idxPlaceHolder;
                BookTrade(trade);
                Log.Instance.WriteEntry(trade.TradingTime + "Mole Signal " + _signal.Id + ": SELL " + _signal.MarketData.Id + " " + price, EventLogEntryType.Information);
                return true;
            }
            return false;
        }

        public bool UpdateIndex(DateTime time, decimal price)
        {
            var signaled = false;
            var addms = 1;
            foreach (var placeHolder in _placeHolders)
            {
                if (placeHolder.Value.Closed || placeHolder.Value.AwaitingTrade)
                    continue;
                var adjustedTime = time.AddMilliseconds(addms++); // this is to keep the trading_time unique
                if (price >= placeHolder.Value.Trade.Price + _stopLoss)
                {
                    Log.Instance.WriteEntry(time + " A stop loss was hit Price " + price, EventLogEntryType.Information);
                    var tradePrice = placeHolder.Value.Trade.Price;
                    ClosePosition(new Trade(placeHolder.Value.Trade, true, adjustedTime), adjustedTime, price);
                    Log.Instance.WriteEntry(time + " A stop loss was hit: Loss " + (price - tradePrice) + " Price " + price, EventLogEntryType.Information);
                    signaled = true;
                }
                else if (price <= placeHolder.Value.Trade.Price - _objective)
                {
                    Log.Instance.WriteEntry(time + " A win was hit: Price " + price, EventLogEntryType.Information);
                    var tradePrice = placeHolder.Value.Trade.Price;
                    ClosePosition(new Trade(placeHolder.Value.Trade, true, adjustedTime), adjustedTime, price);
                    Log.Instance.WriteEntry(time + " A win was hit: Win " + (tradePrice - price) + " Price " + price, EventLogEntryType.Information);
                    signaled = true;
                }
            }
            return signaled;
        }

        public void SetReferenceLevel(decimal wmaValue, Signal signal, MarketLevels? mktLevelsMaybe)
        {
            //_stopLoss = Math.Max(mktLevels.R1 - mktLevels.Pivot, mktLevels.Pivot - mktLevels.S1);            
            _signal = signal;
            _referenceLevel = wmaValue;
            if (!mktLevelsMaybe.HasValue)
                return;
            var mktLevels = mktLevelsMaybe.Value;
            var diff = decimal.MaxValue;
            if (Math.Abs(wmaValue - mktLevels.R3) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R3);
                _referenceLevel = mktLevels.R3;                
            }
            if (Math.Abs(wmaValue - mktLevels.R2) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R2);
                _referenceLevel = mktLevels.R2;
            }
            if (Math.Abs(wmaValue - mktLevels.R1) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R1);
                _referenceLevel = mktLevels.R1;
            }
            if (Math.Abs(wmaValue - mktLevels.Pivot) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.Pivot);
                _referenceLevel = mktLevels.Pivot;
            }
            if (Math.Abs(wmaValue - mktLevels.S1) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S1);
                _referenceLevel = mktLevels.S1;
            }
            if (Math.Abs(wmaValue - mktLevels.S2) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S2);
                _referenceLevel = mktLevels.S2;
            }
            if (Math.Abs(wmaValue - mktLevels.S3) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S3);
                _referenceLevel = mktLevels.S3;
            }          
        }
    }
}
