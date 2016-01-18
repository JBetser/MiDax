using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalANNWMA_4_2 : SignalANN
    {
        Price _startAssetPrice = null;
        MarketLevels _mktLevels;

        public SignalANNWMA_4_2(MarketData asset, List<Indicator> indicators, List<decimal> weights)
            : base(asset, "WMA_4_2", 1, indicators, weights)
        {
            _mktLevels = asset.Levels.Value;
        }

        protected override bool ComputeOutput()
        {
            if (_startAssetPrice == null)
                _startAssetPrice = _asset.TimeSeries.First();
            var updateTime = _indicatorLatestValues.First().Value.Key;
            var amplitude = 100.0m;
            decimal curAssetVal = (_asset.TimeSeries[updateTime].Value.Value.Mid() - _mktLevels.Pivot) / amplitude;

            _inputValues.Add((double)curAssetVal);
            var indicatorId = "WMA_2_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _mktLevels.Pivot) / amplitude));
            indicatorId = "WMA_10_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _mktLevels.Pivot) / amplitude));
            indicatorId = "WMA_60_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)((_indicatorLatestValues[indicatorId].Value.Mid() - _mktLevels.Pivot) / amplitude));
            return true;
        }
    }
}
