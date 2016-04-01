using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelMacDCascade : ModelMacD
    {
        ModelMacD _macD;

        public ModelMacDCascade(ModelMacD macD)
            : base(macD.Index, macD.LowPeriod, macD.MidPeriod, macD.HighPeriod, macD.TradingIndex)
        {
            _macD = macD;
        }

        protected override void Init()
        {
            base.Init();
            _macD_low = new SignalMacDCascade(_index, _macD.SignalLow.IndicatorLow.Period / 60, _macD.SignalHigh.IndicatorLow.Period / 60, _macD.SignalHigh.IndicatorHigh.Period / 60, 1.0m, _macD.SignalHigh.IndicatorLow, _macD.SignalHigh.IndicatorHigh, _tradingIndex);
            _macD_high = new SignalMacDCascade(_index, _macD.SignalLow.IndicatorLow.Period / 60, _macD.SignalHigh.IndicatorLow.Period / 60, _macD.SignalHigh.IndicatorHigh.Period / 60, 2.0m, _macD.SignalHigh.IndicatorLow, _macD.SignalHigh.IndicatorHigh, _tradingIndex);
            _mktSignals = new List<Signal>();
            _mktSignals.Add(_macD_low);
            _mktSignals.Add(_macD_high);
        }
    }

    public class ModelMole : Model
    {
        ModelMacD _macD;
        protected MarketData _daxIndex = null;
        protected SignalMacD _signal = null;
        protected SignalMacD _signalLong = null;
        protected TradingSetMole _tradingSet = null;
        IndicatorWMA _wmaMid = null;
        MarketLevels? _mktLevels;

        public ModelMole(ModelMacD macD)
        {
            _macD = macD;
            _daxIndex = macD.Index;
            _mktLevels = _daxIndex.Levels;            
        }

        protected override void Init()
        {
            base.Init();
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(_daxIndex);
            _mktData = mktData;
            _wmaMid = _macD.SignalLow.IndicatorHigh;
            _signal = new SignalMole(_daxIndex, _macD.SignalLow.IndicatorLow.Period / 60, _macD.SignalLow.IndicatorHigh.Period / 60, _macD.SignalHigh.IndicatorHigh.Period / 60, _macD.SignalLow.IndicatorLow, _macD.SignalLow.IndicatorHigh);
            _mktSignals.Add(_signal);
            _signalLong = new SignalMole(_daxIndex, _macD.SignalHigh.IndicatorLow.Period / 60, _macD.SignalHigh.IndicatorHigh.Period / 60, _macD.SignalHigh.IndicatorHigh.Period / 60, _macD.SignalHigh.IndicatorLow, _macD.SignalHigh.IndicatorHigh);
            _signalLong.Subscribe(new Signal.Tick(LongBuy), new Signal.Tick(LongSell));
            _tradingSet = (TradingSetMole)Portfolio.Instance.GetTradingSet(this);
            _daxIndex.Subscribe(new MarketData.Tick(OnUpdateIndex), null);

            var indicatorLow = new IndicatorLow(_daxIndex);
            var indicatorHigh = new IndicatorHigh(_daxIndex);
            var indicatorCloseBid = new IndicatorCloseBid(_daxIndex);
            var indicatorCloseOffer = new IndicatorCloseOffer(_daxIndex);
            _mktIndicators.Add(new IndicatorNearestLevel(_daxIndex));
            _mktIndicators.Add(indicatorLow);
            _mktIndicators.Add(indicatorHigh);
            _mktIndicators.Add(indicatorCloseBid);
            _mktIndicators.Add(indicatorCloseOffer);
            _mktEODIndicators.Add(indicatorLow);
            _mktEODIndicators.Add(indicatorHigh);
            _mktEODIndicators.Add(indicatorCloseBid);
            _mktEODIndicators.Add(indicatorCloseOffer);
            _mktEODIndicators.Add(new IndicatorLevelPivot(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelR1(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelR2(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelR3(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelS1(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelS2(_daxIndex));
            _mktEODIndicators.Add(new IndicatorLevelS3(_daxIndex));

            _tradingSet.Init(_daxIndex, _signal);
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
        decimal? _referenceLevel;
        int _nbPlaceholders = 0;
        Dictionary<Interval, List<Position>> _placeHolders = new Dictionary<Interval, List<Position>>();
        MarketData _index;

        public bool Ready { get { return _ready; } }

        public TradingSetMole(IAbstractStreamingClient client)
            : base(client)
        {
            _stopLoss = 40.0m;
            _objective = 9.0m;
            _nbPlaceholders = 8;
        }

        public void Init(MarketData index, Signal signal)
        {
            _index = index;
            _signal = signal;
            int nbplaceholders = _nbPlaceholders;
            while(nbplaceholders-- > 0)
                _positions.Add(new Position(index.Id));
            var idxPlaceHolder = 0;
            var placeholderGroup = new Interval(-20.0m, 15.0m);
            _placeHolders[placeholderGroup] = new List<Position>();
            nbplaceholders = _nbPlaceholders;
            while (nbplaceholders-- > 0)
                _placeHolders[placeholderGroup].Add(_positions[idxPlaceHolder++]);
            /*
            _placeHolders[new Interval(0.0m, 2.5m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(0.0m, 2.5m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(2.5m, 5.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(2.5m, 5.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(5.0m, 10.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(5.0m, 10.0m)] = _positions[idxPlaceHolder++];
            _placeHolders[new Interval(10.0m, 15.0m)] = _positions[idxPlaceHolder++];*/
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
            if (!_ready || !Config.TradingOpen(trade.TradingTime) || !_referenceLevel.HasValue)
                return false;
            int idxPlaceHolder = -1;
            foreach (var placeHolderGroup in _placeHolders)
            {
                if (!placeHolderGroup.Key.IsInside(price - _referenceLevel.Value))
                    continue;
                foreach (var placeHolder in placeHolderGroup.Value)
                {
                    idxPlaceHolder++;
                    if (placeHolder.Quantity != 0)
                        continue;
                    trade.PlaceHolder = idxPlaceHolder;
                    BookTrade(trade);
                    Log.Instance.WriteEntry(trade.TradingTime + "Mole Signal " + _signal.Id + ": SELL " + _signal.MarketData.Id + " " + price, EventLogEntryType.Information);
                    return true;
                }
            }
            return false;
        }

        public bool UpdateIndex(DateTime time, decimal price)
        {
            if (!_index.Levels.HasValue)
                return false;
            _referenceLevel = IndicatorNearestLevel.GetNearestLevel(price, _index.Levels.Value);
            var signaled = false;
            var addms = 1;
            foreach (var placeHolderGroup in _placeHolders)
            {
                foreach (var placeHolder in placeHolderGroup.Value)
                {
                    if (placeHolder.AwaitingTrade || placeHolder.Trade == null)
                        continue;
                    var adjustedTime = time.AddMilliseconds(addms++); // this is to keep the trading_time unique
                    if (price >= placeHolder.Trade.Price + _stopLoss)
                    {
                        Log.Instance.WriteEntry(time + " A stop loss was hit Price " + price, EventLogEntryType.Information);
                        var tradePrice = placeHolder.Trade.Price;
                        ClosePosition(new Trade(placeHolder.Trade, true, adjustedTime), adjustedTime, price);
                        Log.Instance.WriteEntry(time + " A stop loss was hit: Loss " + (price - tradePrice) + " Price " + price, EventLogEntryType.Information);
                        signaled = true;
                    }
                    else if (price <= placeHolder.Trade.Price - _objective)
                    {
                        Log.Instance.WriteEntry(time + " A win was hit: Price " + price, EventLogEntryType.Information);
                        var tradePrice = placeHolder.Trade.Price;
                        ClosePosition(new Trade(placeHolder.Trade, true, adjustedTime), adjustedTime, price);
                        Log.Instance.WriteEntry(time + " A win was hit: Win " + (tradePrice - price) + " Price " + price, EventLogEntryType.Information);
                        signaled = true;
                    }
                }
            }
            return signaled;
        }
    }
}
