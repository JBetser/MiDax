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
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 0.5m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 1.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 2.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity < 0)
            {
                signal.Trade.Price = value.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + value.Offer, EventLogEntryType.Information);
            }
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + value.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + value.Bid, EventLogEntryType.Information);
                }
            }
        }
    }

    public class ModelMole : Model
    {
        protected MarketData _daxIndex = null;
        protected SignalMacD _signal = null;
        TradingSetMole _tradingSet = null;
        IndicatorWMA _wmaHigh = null;
        MarketLevels? _mktLevels;

        public ModelMole(ModelMacD macD)
        {
            _daxIndex = macD.Index;
            _wmaHigh = macD.SignalHigh.IndicatorHigh;
            _mktLevels = _daxIndex.Levels;
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(_daxIndex);
            _mktData = mktData;
            _signal = new SignalMole(_daxIndex, macD.SignalLow.IndicatorLow.Period / 60, macD.SignalLow.IndicatorHigh.Period / 60, macD.SignalLow.IndicatorLow, macD.SignalLow.IndicatorHigh);
            _mktSignals.Add(_signal);
            _tradingSet = (TradingSetMole)Portfolio.Instance.GetTradingSet(this);
            _wmaHigh.Subscribe(new MarketData.Tick(OnUpdateWMA));            
            _tradingSet.Init(_daxIndex);
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity < 0)
            {
                signal.Trade.Price = value.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Mole MacD Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + value.Offer, EventLogEntryType.Information);
            }
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Mole MacD Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + value.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    if (_tradingSet.PlaceTrade(signal.Trade, value.Bid))
                    {
                        string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                        Log.Instance.WriteEntry(time + tradeRef + " Mole MacD Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + value.Bid, EventLogEntryType.Information);
                    }
                }
            }
        }

        protected void OnUpdateWMA(MarketData mktData, DateTime updateTime, Price value)
        {
            _tradingSet.SetReferenceLevel(mktData.Levels.Value, value.Mid());
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
        public bool Free = true;
        public Interval(decimal min, decimal max)
        {
            Min = min;
            Max = max;
        }
    }

    public class TradingSetMole : TradingSet
    {
        decimal _referenceLevel = 0.0m;
        bool _ready = false;
        Dictionary<Interval, Position> _placeHolders = new Dictionary<Interval, Position>();

        public bool Ready { get { return _ready; } }

        public TradingSetMole(IAbstractStreamingClient client)
            : base(client)
        {
        }

        public void Init(MarketData index)
        {
            _positions.Add(new Position(index.Id));
            _positions.Add(new Position(index.Id));
            _positions.Add(new Position(index.Id));
            _positions.Add(new Position(index.Id));
            _placeHolders[new Interval(0.0m, 5.0m)] = _positions[0];
            _placeHolders[new Interval(5.0m, 10.0m)] = _positions[1];
            _placeHolders[new Interval(10.0m, 15.0m)] = _positions[2];
            _placeHolders[new Interval(15.0m, 20.0m)] = _positions[3];
        }

        public bool PlaceTrade(Trade trade, decimal price)
        {
            if (!_ready)
                return false;
            int idxPlaceHolder = 0;
            foreach (var placeHolder in _placeHolders)
            {
                if (price >= _referenceLevel + placeHolder.Key.Min && price < _referenceLevel + placeHolder.Key.Max)
                {
                    if (placeHolder.Key.Free)
                    {
                        placeHolder.Key.Free = false;
                        BookTrade(trade, idxPlaceHolder);
                        return true;
                    }
                }
                idxPlaceHolder++;
            }
            return false;
        }

        public void SetReferenceLevel(MarketLevels mktLevels, decimal wmaValue)
        {
            _stopLoss = Math.Max(mktLevels.R1 - mktLevels.Pivot, mktLevels.Pivot - mktLevels.S1);            
            var diff = decimal.MaxValue;
            if (Math.Abs(wmaValue - mktLevels.R3) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R3);
                _referenceLevel = mktLevels.R3;                
            }
            else if (Math.Abs(wmaValue - mktLevels.R2) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R2);
                _referenceLevel = mktLevels.R2;
            }
            else if (Math.Abs(wmaValue - mktLevels.R1) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.R1);
                _referenceLevel = mktLevels.R1;
            }
            else if (Math.Abs(wmaValue - mktLevels.Pivot) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.Pivot);
                _referenceLevel = mktLevels.Pivot;
            }
            else if (Math.Abs(wmaValue - mktLevels.S1) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S1);
                _referenceLevel = mktLevels.S1;
            }
            else if (Math.Abs(wmaValue - mktLevels.S2) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S2);
                _referenceLevel = mktLevels.S2;
            }
            else if (Math.Abs(wmaValue - mktLevels.S3) < diff)
            {
                diff = Math.Abs(wmaValue - mktLevels.S3);
                _referenceLevel = mktLevels.S3;
            }

            _ready = true;
        }
    }
}
