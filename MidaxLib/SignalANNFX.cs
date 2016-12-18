using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalANNFX_5_2 : SignalANN
    {
        decimal _startAssetPrice = -1m;
        DateTime _tradingStart;

        public SignalANNFX_5_2(MarketData asset, List<Indicator> indicators, List<decimal> weights)
            : base(asset, "FX_5_2", 1, indicators, weights)
        {
            _tradingStart = Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]);
        }

        protected override bool ComputeOutput()
        {            
            var updateTime = _indicatorLatestValues.First().Value.Key;
            if (updateTime.TimeOfDay < _tradingStart.TimeOfDay)
                return false;
            if (_startAssetPrice == -1m)
                _startAssetPrice = _indicatorLatestValues["EMA_90_" + _asset.Id].Value.Mid();

            var amplitude = 100.0m;
            decimal curAssetVal = (_asset.TimeSeries[updateTime].Value.Value.Mid() - _startAssetPrice) / amplitude;
            _inputValues.Add((double)curAssetVal);
            var indicatorId = "EMA_2_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            indicatorId = "EMA_10_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            indicatorId = "EMA_30_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            indicatorId = "EMA_90_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            return true;
        }
    }

    public class SignalANNFX_6_2 : SignalANN
    {
        decimal _startAssetPrice = -1m;
        DateTime _tradingStart;

        public SignalANNFX_6_2(MarketData asset, List<Indicator> indicators, List<decimal> weights)
            : base(asset, "FX_6_2", 1, indicators, weights)
        {
            _tradingStart = Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]);
        }

        protected override bool ComputeOutput()
        {
            var updateTime = _indicatorLatestValues.First().Value.Key;
            if (updateTime.TimeOfDay < _tradingStart.TimeOfDay)
                return false;
            if (_startAssetPrice == -1m)
                _startAssetPrice = _indicatorLatestValues["EMA_90_" + _asset.Id].Value.Mid();

            var amplitude = 100.0m;
            decimal curAssetVal = (_asset.TimeSeries[updateTime].Value.Value.Mid() - _startAssetPrice) / amplitude;
            _inputValues.Add((double)curAssetVal);
            var indicatorId = "EMA_10_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            indicatorId = "EMA_90_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice) / amplitude));
            return true;
        }
    }
}
