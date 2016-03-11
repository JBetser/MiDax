using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public enum DOW_STOCK
    {
        MMM = 0,
        AXP = 1,
        AAPL = 2,
        BA = 3,
        CAT = 4,
        CVX = 5,
        CSCO = 6,
        KO = 7,
        DIS = 8,
        DD = 9,
        XOM = 10,
        GE = 11,
        GS = 12,
        HD = 13,
        IBM = 14,
        INTC = 15,
        JNJ = 16,
        JPM = 17,
        MCD = 18,
        MRK = 19,
        MSFT = 20,
        NKE = 21,
        PFE = 22,
        PG = 23,
        TRV = 24,
        UTX = 25,
        UNH = 26,
        VZ = 27,
        V = 28,
        WMT = 29
    }

    public class IceStreamingMarketData : MarketData
    {
        static IceStreamingMarketData _instance;
        protected decimal[] _stockLastPrices = null;
        protected decimal[] _stockLastVolumes = null;
        protected decimal[] _stockWeights = null;
        protected decimal _indexWeight = 1.0m;
        DateTime? _lastTradePrice = null;
        bool _isReady = false;

        public bool Ready { get { return _isReady; } }

        static public IceStreamingMarketData Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                    _instance = new IceStreamingDow();
                return _instance;
            }
        }

        protected IceStreamingMarketData(string name)
            : base(name)
        {          
        }

        public void OnTick(int stockId, DateTime dt, decimal price, decimal volume)
        {
            _stockLastPrices[stockId] = price;
            _stockLastVolumes[stockId] = volume;            
            if (_isReady)
            {
                var idxPriceValue = 0m;
                for (int idx = 0; idx < 30; idx++)
                    idxPriceValue += _stockLastPrices[idx];
                idxPriceValue *= _indexWeight;
                var idxPrice = new Price(idxPriceValue, idxPriceValue, volume * _stockWeights[stockId]);
                _values.Add(dt, idxPrice);
                if (_lastTradePrice.HasValue)
                {
                    if (dt > _lastTradePrice.Value)
                        _lastTradePrice = dt;                        
                    else
                        dt = _lastTradePrice.Value;                        
                }
                else
                    _lastTradePrice = dt;
                FireTick(dt, idxPrice);
            }
            else
            {
                var isReady = true;
                foreach (var ts in _stockLastPrices)
                {
                    if (ts < 0)
                    {
                        isReady = false;
                        break;
                    }
                }
                if (isReady)
                    Log.Instance.WriteEntry("IceConnection: all stocks have been registered", EventLogEntryType.Information);
                _isReady = isReady;
            }
        }

        public override void Subscribe(Tick updateHandler, Tick tickerHandler)
        {
            _allPositionsClosed = false;
            _closePositionTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_STOP_TIME"]);
            Clear();
            bool subscribe = (this._updateHandlers.Count == 0);
            _updateHandlers.Add(updateHandler);
            if (tickerHandler != null)
                _tickHandlers.Add(tickerHandler);
        }

        public override void Unsubscribe(Tick updateHandler, Tick tickerHandler)
        {
            _updateHandlers.Remove(updateHandler);
            if (tickerHandler != null)
                _updateHandlers.Remove(tickerHandler);
        }
    }

    public class IceStreamingDow : IceStreamingMarketData
    {
        public IceStreamingDow()
            : base("DOW:IceConnection.DOW")
        {
            _stockLastPrices = new decimal[30];
            _stockLastVolumes = new decimal[30];
            _stockWeights = new decimal[30];
            for (int idx = 0; idx < 30; idx++)
            {
                _stockLastPrices[idx] = -1.0m;
                _stockLastVolumes[idx] = -1.0m;
                _stockWeights[idx] = -1.0m;
            }
            _indexWeight = 1.0m / 0.14602128057775m;

            _stockWeights[(int)DOW_STOCK.MMM] = 0.0647m;
            _stockWeights[(int)DOW_STOCK.GS] = 0.0612m;
            _stockWeights[(int)DOW_STOCK.IBM] = 0.0561m;
            _stockWeights[(int)DOW_STOCK.HD] = 0.0512m;
            _stockWeights[(int)DOW_STOCK.BA] = 0.0494m;
            _stockWeights[(int)DOW_STOCK.UNH] = 0.0491m;
            _stockWeights[(int)DOW_STOCK.MCD] = 0.0478m;
            _stockWeights[(int)DOW_STOCK.TRV] = 0.0447m;
            _stockWeights[(int)DOW_STOCK.JNJ] = 0.0429m;
            _stockWeights[(int)DOW_STOCK.AAPL] = 0.0408m;
            _stockWeights[(int)DOW_STOCK.DIS] = 0.0395m;
            _stockWeights[(int)DOW_STOCK.UTX] = 0.0391m;
            _stockWeights[(int)DOW_STOCK.CVX] = 0.0358m;
            _stockWeights[(int)DOW_STOCK.PG] = 0.0335m;
            _stockWeights[(int)DOW_STOCK.XOM] = 0.0334m;
            _stockWeights[(int)DOW_STOCK.CAT] = 0.0290m;
            _stockWeights[(int)DOW_STOCK.VZ] = 0.0285m;
            _stockWeights[(int)DOW_STOCK.WMT] = 0.0275m;
            _stockWeights[(int)DOW_STOCK.DD] = 0.0256m;
            _stockWeights[(int)DOW_STOCK.NKE] = 0.0256m;
            _stockWeights[(int)DOW_STOCK.AXP] = 0.0241m;
            _stockWeights[(int)DOW_STOCK.JPM] = 0.0240m;
            _stockWeights[(int)DOW_STOCK.VZ] = 0.0237m;
            _stockWeights[(int)DOW_STOCK.MRK] = 0.0212m;
            _stockWeights[(int)DOW_STOCK.MSFT] = 0.0212m;
            _stockWeights[(int)DOW_STOCK.KO] = 0.0209m;
            _stockWeights[(int)DOW_STOCK.INTC] = 0.0179m;
            _stockWeights[(int)DOW_STOCK.GE] = 0.0123m;
            _stockWeights[(int)DOW_STOCK.PFE] = 0.0119m;
            _stockWeights[(int)DOW_STOCK.CSCO] = 0.0109m;
        }
    }
}
