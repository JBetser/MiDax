using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public abstract class SignalANN : Signal
    {
        protected NeuralNetwork _ann = null;
        protected Dictionary<string,KeyValuePair<DateTime,Price>> _indicatorLatestValues = null;
        protected List<double> _inputValues = null;

        public SignalANN(MarketData asset, string annid, int version, List<Indicator> indicators, List<decimal> weights)
            : base("ANN_" + annid + "_" + version.ToString() + "_" + asset.Id, asset)
        {
            string[] anncomponents = annid.Split('_');
            _ann = new NeuralNetwork(int.Parse(anncomponents[1]), 1, anncomponents[2].Split('#').Select(str => int.Parse(str)).ToList(), weights); 
            _mktIndicator = indicators;
            _indicatorLatestValues = new Dictionary<string,KeyValuePair<DateTime,Price>>();
            _inputValues = new List<double>();
        }

        protected abstract bool ComputeOutput();

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            _indicatorLatestValues[indicator.Id] = new KeyValuePair<DateTime,Price>(updateTime,value);

            DateTime dt = DateTime.MinValue;
            foreach (var ind in _indicatorLatestValues)
            {
                if (dt == DateTime.MinValue)
                {
                    dt = ind.Value.Key;
                    continue;
                }
                if (dt != ind.Value.Key)
                    return false;
            }

            _inputValues.Clear();
            if (!ComputeOutput())
                return false;

            _ann.CalculateOutput(_inputValues);
            var output = _ann.GetOutput()[0];

            if (output > 0.5 && _signalCode != SIGNAL_CODE.BUY)
            {
                tradingOrder = _onBuy;
                _signalCode = SIGNAL_CODE.BUY;
            }
            else if (output < -0.5 && _signalCode != SIGNAL_CODE.SELL)
            {
                tradingOrder = _onSell;
                _signalCode = SIGNAL_CODE.SELL;
            }
            else{
                tradingOrder = _onHold;
                _signalCode = SIGNAL_CODE.HOLD;
            }
            return true;
        }
    }
}
