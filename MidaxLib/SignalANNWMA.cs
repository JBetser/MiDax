using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalANNWMA_4_2 : SignalANN
    {
        DateTime _startTime = DateTime.MinValue;
        Price _startAssetPrice = null;

        public SignalANNWMA_4_2(MarketData asset, List<Indicator> indicators, List<decimal> weights)
            : base(asset, "WMA_4_2", 1, indicators, weights)
        {
            _startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
        }

        protected override bool ComputeOutput()
        {
            if (_startAssetPrice == null)
                _startAssetPrice = _asset.TimeSeries.First();
            var updateTime = _indicatorLatestValues.First().Value.Key;
            Price curAssetVal = _asset.TimeSeries[updateTime].Value.Value - _startAssetPrice;

            _inputValues.Add((double)curAssetVal.Mid());
            var indicatorId = "WMA_2_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)(_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice.Mid()));
            indicatorId = "WMA_10_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)(_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice.Mid()));
            indicatorId = "WMA_60_" + _asset.Id;
            if (!_indicatorLatestValues.ContainsKey(indicatorId))
                return false;
            _inputValues.Add((double)(_indicatorLatestValues[indicatorId].Value.Mid() - _startAssetPrice.Mid()));
            return true;
        }
    }
}
