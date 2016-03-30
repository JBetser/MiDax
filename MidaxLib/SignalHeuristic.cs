using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalMacDCascade : SignalMacD
    {
        decimal _threshold = 0.0m;
        decimal _pivot = 0.0m;
        decimal _localMinimum = 0.0m;
        decimal _localMaximum = 0.0m;
        bool _cascading = false;
        bool _buying = false;
        bool _selling = false;
        
        public SignalMacDCascade(MarketData asset, int verylowPeriod, int lowPeriod, int highPeriod, decimal threshold, IndicatorWMA low = null, IndicatorWMA high = null)
            : base("MacDCas_" + verylowPeriod + "_" + lowPeriod + "_" + highPeriod + "_" + (int)decimal.Round(threshold * 100.0m) + "_" + asset.Id, asset, lowPeriod, highPeriod, low, high)
        {
            _threshold = threshold;
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            if (base.Process(indicator, updateTime, value, ref tradingOrder))
            {
                if (_trendAssumption != SIGNAL_CODE.BUY && tradingOrder == _onSell)
                {
                    if (_low.TimeSeries.Count >= 2)
                    {
                        var lowVal = _low.TimeSeries[updateTime].Value.Value;
                        var prevValues = _low.TimeSeries.Values(updateTime, new TimeSpan(0, 1, 0));
                        if (prevValues.Count >= 2)
                        {
                            var prevVal = prevValues[prevValues.Count - 2].Value;
                            if (_cascading)
                            {
                                if (_localMinimum > lowVal.Bid)
                                    _localMinimum = lowVal.Bid;
                                if (_localMaximum < lowVal.Bid)
                                    _localMaximum = lowVal.Bid;
                                if (_buying)
                                {
                                    if (lowVal.Bid < _localMaximum - _threshold)
                                    {
                                        _pivot = _localMaximum;
                                        _localMinimum = _localMaximum;
                                        _buying = false;
                                    }
                                    else if (lowVal.Bid > _pivot - _threshold)
                                    {
                                        _signalCode = SIGNAL_CODE.BUY;
                                        tradingOrder = _onBuy;
                                    }
                                }
                                else
                                {
                                    if (lowVal.Bid > _localMinimum + _threshold)
                                    {
                                        _pivot = _localMinimum;
                                        _localMaximum = _localMinimum;
                                        _signalCode = SIGNAL_CODE.BUY;
                                        tradingOrder = _onBuy;
                                        _buying = true;
                                    }
                                }
                            }
                            else
                            {
                                _cascading = true;
                                _localMinimum = lowVal.Bid;
                                _localMaximum = lowVal.Bid;
                                _pivot = _localMaximum;
                                _buying = false;
                            }
                            return true;
                        }
                    }
                }
                else if (_trendAssumption != SIGNAL_CODE.SELL && tradingOrder == _onBuy)
                {
                    if (_low.TimeSeries.Count >= 2)
                    {
                        var lowVal = _low.TimeSeries[updateTime].Value.Value;
                        var prevValues = _low.TimeSeries.Values(updateTime, new TimeSpan(0, 1, 0));
                        if (prevValues.Count >= 2)
                        {
                            var prevVal = prevValues[prevValues.Count - 2].Value;
                            if (_cascading)
                            {
                                if (_localMinimum > lowVal.Offer)
                                    _localMinimum = lowVal.Offer;
                                if (_localMaximum < lowVal.Offer)
                                    _localMaximum = lowVal.Offer;
                                if (_selling)
                                {
                                    if (lowVal.Offer > _localMinimum + _threshold)
                                    {
                                        _pivot = _localMinimum;
                                        _localMaximum = _localMinimum;
                                        _selling = false;
                                    }
                                    else if (lowVal.Offer < _pivot - _threshold)
                                    {
                                        _signalCode = SIGNAL_CODE.SELL;
                                        tradingOrder = _onSell;
                                    }
                                }
                                else
                                {
                                    if (lowVal.Offer < _localMaximum - _threshold)
                                    {
                                        _pivot = _localMaximum;
                                        _localMinimum = _localMaximum;
                                        _signalCode = SIGNAL_CODE.SELL;
                                        tradingOrder = _onSell;
                                        _selling = true;
                                    }
                                }
                            }
                            else
                            {
                                _cascading = true;
                                _localMinimum = lowVal.Offer;
                                _localMaximum = lowVal.Offer;
                                _pivot = _localMinimum;
                                _selling = false;
                            }
                            return true;
                        }
                    }
                }
                _cascading = false;
                _buying = false;
                _selling = false;
                return true;
            }
            return false;
        }
    }

    public class SignalMole : SignalMacD
    {
        public SignalMole(MarketData asset, int lowPeriod, int midPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : base("Mole_" + lowPeriod + "_" + midPeriod + "_" + highPeriod + "_" + asset.Id, asset, lowPeriod, midPeriod, low, high)
        {
        }
    }
}
