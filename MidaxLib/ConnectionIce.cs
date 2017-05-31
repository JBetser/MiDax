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
        MMM     = 0,
        AXP     = 1,
        AAPL    = 2,
        BA      = 3,
        CAT     = 4,
        CVX     = 5,
        CSCO    = 6,
        KO      = 7,
        DIS     = 8,
        DD      = 9,
        XOM     = 10,
        GE      = 11,
        GS      = 12,
        HD      = 13,
        IBM     = 14,
        INTC    = 15,
        JNJ     = 16,
        JPM     = 17,
        MCD     = 18,
        MRK     = 19,
        MSFT    = 20,
        NKE     = 21,
        PFE     = 22,
        PG      = 23,
        TRV     = 24,
        UTX     = 25,
        UNH     = 26,
        VZ      = 27,
        V       = 28,
        WMT     = 29
    }

    public abstract class IceStreamingMarketData : MarketData
    {
        static IceStreamingMarketData _instance;
        protected string _symbolId;
        
        protected decimal[] _stockLastPrices = null;
        protected decimal[] _stockLastVolumes = null;
        protected decimal[] _stockWeights = null;
        protected decimal _indexWeight = 1.0m;
        protected DateTime? _lastTradeTime = null;   

        static public IceStreamingMarketData Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                    _instance = new IceStreamingDow(Config.Settings["INDEX_ICEDOW"], Config.Settings["INDEX_DOW"]);
                return _instance;
            }
        }

        protected IceStreamingMarketData(string name, string mktLevelsId)
            : base(name, mktLevelsId)
        {
            _symbolId = name.Split('.')[1];
        }

        public abstract void OnTick(string stockId, DateTime dt, decimal price, decimal volume);

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
        public IceStreamingDow(string name, string mktLevelsId)
            : base(name, mktLevelsId)
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

            _stockWeights[(int)DOW_STOCK.GS] = 0.0730m;
            _stockWeights[(int)DOW_STOCK.MMM] = 0.0644m;
            _stockWeights[(int)DOW_STOCK.BA] = 0.0604m;
            _stockWeights[(int)DOW_STOCK.UNH] = 0.0572m;
            _stockWeights[(int)DOW_STOCK.HD] = 0.0505m;
            _stockWeights[(int)DOW_STOCK.AAPL] = 0.0500m;
            _stockWeights[(int)DOW_STOCK.IBM] = 0.0497m;
            _stockWeights[(int)DOW_STOCK.MCD] = 0.0487m;
            _stockWeights[(int)DOW_STOCK.JNJ] = 0.0416m;
            _stockWeights[(int)DOW_STOCK.UTX] = 0.0398m;
            _stockWeights[(int)DOW_STOCK.TRV] = 0.0398m;
            _stockWeights[(int)DOW_STOCK.DIS] = 0.0351m;
            _stockWeights[(int)DOW_STOCK.CVX] = 0.0346m;
            _stockWeights[(int)DOW_STOCK.CAT] = 0.0339m;
            _stockWeights[(int)DOW_STOCK.V] = 0.0309m;
            _stockWeights[(int)DOW_STOCK.PG] = 0.0282m;
            _stockWeights[(int)DOW_STOCK.JPM] = 0.0279m;
            _stockWeights[(int)DOW_STOCK.XOM] = 0.0268m;
            _stockWeights[(int)DOW_STOCK.DD] = 0.0255m;
            _stockWeights[(int)DOW_STOCK.WMT] = 0.0255m;
            _stockWeights[(int)DOW_STOCK.AXP] = 0.0251m;
            _stockWeights[(int)DOW_STOCK.MSFT] = 0.0224m;
            _stockWeights[(int)DOW_STOCK.MRK] = 0.0212m;
            _stockWeights[(int)DOW_STOCK.NKE] = 0.0170m;
            _stockWeights[(int)DOW_STOCK.VZ] = 0.0147m;
            _stockWeights[(int)DOW_STOCK.KO] = 0.0147m;
            _stockWeights[(int)DOW_STOCK.INTC] = 0.0118m;
            _stockWeights[(int)DOW_STOCK.PFE] = 0.0104m;
            _stockWeights[(int)DOW_STOCK.CSCO] = 0.0103m;
            _stockWeights[(int)DOW_STOCK.GE] = 0.0091m;
            
        }

        public override void OnTick(string stockId, DateTime dt, decimal price, decimal volume)
        {
            DOW_STOCK stock;
            if (!Enum.TryParse(stockId, true, out stock))
            {
                Log.Instance.WriteEntry("IceConnection: " + stockId + " is not a DOW component", EventLogEntryType.Error);
                return;
            }
            int stockIdx = (int)stock;
            _stockLastPrices[stockIdx] = price;
            _stockLastVolumes[stockIdx] = volume;
            if (_isReady)
            {
                var idxPriceValue = 0m;
                for (int idx = 0; idx < 30; idx++)
                    idxPriceValue += _stockLastPrices[idx];
                idxPriceValue *= _indexWeight;
                var idxPrice = new Price(idxPriceValue, idxPriceValue, volume * _stockWeights[stockIdx]);

                if (_lastTradeTime.HasValue)
                {
                    if (dt > _lastTradeTime.Value)
                        _lastTradeTime = dt;
                }
                else
                    _lastTradeTime = dt;
                FireTick(_lastTradeTime.Value, idxPrice);
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
                    Log.Instance.WriteEntry("IceConnection: " + _symbolId + " has been registered", EventLogEntryType.Information);
                _isReady = isReady;
            }
        }
    }
}
