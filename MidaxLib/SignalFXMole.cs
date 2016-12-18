using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalFXMole : Signal
    {
        decimal _startThreshold = 0.0m;
        decimal? _startValue = null;
        decimal _nominalVol = 0m;
        decimal _highVol = 0m;
        int _timeFrameMn = 0;
        int _timeFramePeakMn = 0;
        int _timeFrameBottomMn = 0;
        int _timeFrameStopLossMn = 0;
        int _timeFrameRsiLongMn = 0;
        decimal _rsiBuyThreshold = 0m;
        decimal _rsiSellThreshold = 100m;
        decimal _maxVol = 0m;
        decimal _maxLongVol = 0m;
        decimal _maxTotalVol = 0m;
        decimal _minVol = 0m;
        bool _rsi_loss_reset = false;
        bool _stopTrading = false;
        DateTime _tradingStart = DateTime.MinValue;
        decimal _tradingStartValue = 0m;
        decimal _maxSpread = 0m;
        decimal _maxShortMacDSpread = 0m;
        decimal _maxLongMacDSpread = 0m;
        decimal _tradingTrend = 0m;
        decimal _maxWmVol = 0m;
        //IndicatorRSI _rsiLong;
        IndicatorEMA _emaVeryShort;
        IndicatorEMA _emaShort;
        IndicatorEMA _emaLong;
        IndicatorTrend _trend;
        IndicatorWMVol _wmvol;
        //IndicatorWMVol _wmvolLong;
        IndicatorTrend _volTrend;
        //IndicatorEMA _volTrendAvg;
        IndicatorTrend _volTrendTrend;
        //IndicatorCurve _volCurve;
        List<Interval> stopLossTimes = new List<Interval>();
        List<Interval> stopLossValues = new List<Interval>();
        List<Interval> bigStopLossValues = new List<Interval>();
        List<Interval> stopWinValues = new List<Interval>();
        IndicatorRSI[] _rsiRefs;
		Calendar _calendar = null;

        public SignalFXMole(MarketData fx, IndicatorRSI rsi, ModelMacD macD, IndicatorRSI[] rsiRefs, decimal volCoeff)
            : base("FXMole_" + rsi.PeriodSizeMn + "_" + rsi.NbPeriods + "_" + fx.Id, fx)
        {
            _startThreshold = 2m;
            _timeFrameMn = 90;
            _timeFramePeakMn = 75;
            _timeFrameBottomMn = 30;
            _timeFrameStopLossMn = 30;
            _timeFrameRsiLongMn = 5;
            _rsiBuyThreshold = 30m;
            _rsiSellThreshold = 70m;
            _minVol = 15m * volCoeff;
            _maxVol = 30m * volCoeff;
            _maxLongVol = 50m * volCoeff;
            _maxTotalVol = 100m * volCoeff;
            _rsi_loss_reset = true;
            _emaVeryShort = macD.SignalLow.IndicatorLow;
            _emaShort = macD.SignalHigh.IndicatorLow;
            _emaLong = macD.SignalHigh.IndicatorHigh;
            _rsiRefs = rsiRefs;
            _nominalVol = 10m * volCoeff;
            _highVol = 25m * volCoeff;
            _maxSpread = volCoeff * 1.20m; // max +20% spread
            _maxShortMacDSpread = 5m * volCoeff;
            _maxLongMacDSpread = 10m * volCoeff;
            _maxWmVol = 2m * volCoeff;
            //_rsiLong = new IndicatorRSI(fx, rsi.PeriodSizeMn, rsi.NbPeriods * 2);
            _trend = new IndicatorTrend(fx, rsi.PeriodSizeMn * 30, rsi.NbPeriods, false);
            _wmvol = new IndicatorWMVol(fx, _emaVeryShort, 60, 90);
            //_wmvolLong = new IndicatorWMVol(fx, _emaLong);
            _volTrend = new IndicatorTrend(_wmvol, 30, 6, true);
            //_volTrendAvg = new IndicatorEMA(_wmvol, _timeFrameMn);
            _volTrendTrend = new IndicatorTrend(_volTrend, 30, 6, true);
            //_volCurve = new IndicatorCurve(_volTrend);

            for (int idxInterval = 0; idxInterval < _timeFrameStopLossMn; idxInterval++)
                stopLossTimes.Add(new Interval(idxInterval, idxInterval + 1));
            decimal prevStopLoss = _nominalVol;
            for (int idxLin = 0; idxLin < _timeFrameStopLossMn; idxLin++)
            {
                decimal stopLoss = _nominalVol * (1m - (decimal)idxLin / _timeFrameStopLossMn);
                stopLossValues.Add(new Interval(prevStopLoss, stopLoss));
                stopWinValues.Add(new Interval(prevStopLoss, stopLoss));
                prevStopLoss = stopLoss;
            }
            for (int idxLin = 0; idxLin <= _timeFrameStopLossMn; idxLin++)
                bigStopLossValues.Add(new Interval(_highVol, _highVol));
            _trend.Subscribe(OnUpdateTrend, null);            
            //_rsiLong.Subscribe(OnUpdateRsiLong, null);
            _wmvol.Subscribe(OnUpdateWmVol, null);
            //_wmvolLong.Subscribe(OnUpdateWmVolLong, null);
            _mktIndicator.Add(rsi);
            _signalCode = SIGNAL_CODE.HOLD;
        }

        decimal getBuyThreshold()
        {
            return _rsiBuyThreshold;
        }

        decimal getSellThreshold()
        {
            return _rsiSellThreshold;
        }

        void OnUpdateTrend(MarketData mktData, DateTime updateTime, Price value)
        {
            Price trend = _trend.CalcTrend(_asset, updateTime);
            if (trend != null)
                _trend.Publish(updateTime, trend.Bid);                
        }

        void OnUpdateVolTrend(MarketData mktData, DateTime updateTime, Price value)
        {
            Price trendVol = _volTrend.CalcTrend(_wmvol, updateTime);
            if (trendVol != null)
            {
                _volTrend.TimeSeries.Add(updateTime, trendVol);
                _volTrend.Publish(updateTime, trendVol.Bid);
            }
        }

        void OnUpdateVolTrendTrend(MarketData mktData, DateTime updateTime, Price value)
        {
            Price volCurve = _volTrendTrend.CalcTrend(_volTrend, updateTime);
            if (volCurve != null)
            {
                _volTrendTrend.TimeSeries.Add(updateTime, volCurve);
                _volTrendTrend.Publish(updateTime, volCurve.Bid);
            }
        }

        void OnUpdateWmVol(MarketData mktData, DateTime updateTime, Price value)
        {
            Price wmvol = _wmvol.CalcWMVol(_asset, updateTime, value);
            if (wmvol != null)
            {
                _wmvol.Publish(updateTime, wmvol.Bid);
                OnUpdateVolTrend(mktData, updateTime, value);
                OnUpdateVolTrendTrend(mktData, updateTime, value);
            }
        }

        /*
        void OnUpdateVolTrendAvg(MarketData mktData, DateTime updateTime, Price value)
        {
            Price trendVol = _volTrendAvg.Average(updateTime);
            if (trendVol != null)
                _volTrendAvg.Publish(updateTime, trendVol.Bid);
        }
        
        void OnUpdateVolCurve(MarketData mktData, DateTime updateTime, Price value)
        {
            Price volCurve = _volCurve.CalcCurve(_volTrend, updateTime);
            if (volCurve != null)
                _volCurve.Publish(updateTime, volCurve.Bid);
        } 
        
        void OnUpdateRsiLong(MarketData mktData, DateTime updateTime, Price value)
        {
            Price rsi = _rsiLong.CalcRSI(_asset, updateTime);
            if (rsi != null)
                _rsiLong.Publish(updateTime, rsi.Bid);
        }        

        void OnUpdateWmVolLong(MarketData mktData, DateTime updateTime, Price value)
        {
            Price wmvol = _wmvolLong.CalcWMVol(_asset, updateTime, value);
            if (wmvol != null)
                _wmvolLong.Publish(updateTime, wmvol.Bid);
        }*/

        bool close(string reason, ref Signal.Tick tradingOrder)
        {
            if (_signalCode == SIGNAL_CODE.BUY)
            {
                tradingOrder = _onSell;
                _signalCode = SIGNAL_CODE.SELL;
            }
            else if (_signalCode == SIGNAL_CODE.SELL)
            {
                tradingOrder = _onBuy;
                _signalCode = SIGNAL_CODE.BUY;
            }
            else
            {
                _signalCode = SIGNAL_CODE.FAILED;
                Log.Instance.WriteEntry(_id + " error detected while attempting to close a position");
                return false;
            }
            _tradingStart = DateTime.MinValue;
            _tradingStartValue = 0m;
            Log.Instance.WriteEntry(_id + reason);
            return true;
        }

        public void Reset()
        {
            Log.Instance.WriteEntry(_id + " has been reset due to market unavailable or some booking problem", System.Diagnostics.EventLogEntryType.Warning);
            _tradingStart = DateTime.MinValue;
            _tradingStartValue = 0m;
            _signalCode = SIGNAL_CODE.HOLD;
            _rsi_loss_reset = false;
        }

        string _lastReason;

        void WriteBlockReason(DateTime updateTime, string reason)
        {
            if (reason != _lastReason)
            {
                _lastReason = reason;
                Log.Instance.WriteEntry(_id + " " + updateTime + ": " + reason);
            }
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            if (_calendar == null)
                _calendar = new Calendar(updateTime);
            IndicatorRSI rsi = (IndicatorRSI)indicator;
            if (indicator.TimeSeries.TotalMinutes(updateTime) < _timeFrameMn)
                return false;
            if (!_startValue.HasValue)
                _startValue = value.Mid();
            Price curRsi = rsi.TimeSeries.Last();
            Price prevRsi = rsi.TimeSeries.PrevValue(updateTime).Value.Value;
            if ((prevRsi.Bid <= 50m && curRsi.Bid >= 50m) || (curRsi.Bid <= 50m && prevRsi.Bid >= 50m))
                _rsi_loss_reset = true;
            //_rsiSellThreshold += Math.Max(0m, curRsi.Bid - (decimal)getSellThreshold()) / 2m;
            //_rsiBuyThreshold += Math.Min(0m, curRsi.Bid - (decimal)getBuyThreshold()) / 2m;
            var minVal = _asset.TimeSeries.Min(updateTime.AddMinutes(-_timeFrameStopLossMn), updateTime);
            var maxVal = _asset.TimeSeries.Max(updateTime.AddMinutes(-_timeFrameStopLossMn), updateTime);
            if (maxVal - minVal > _maxVol)
            {
                //WriteBlockReason(updateTime, string.Format("reset due to vol > {0}", _maxVol));
                _rsi_loss_reset = false;
            }
            minVal = _asset.TimeSeries.Min(updateTime.AddMinutes(-_timeFrameBottomMn), updateTime);
            maxVal = _asset.TimeSeries.Max(updateTime.AddMinutes(-_timeFrameBottomMn), updateTime);
            if (maxVal - minVal > _maxLongVol)
            {
                //WriteBlockReason(updateTime, string.Format("reset due to long vol > {0}", _maxLongVol));
                _rsi_loss_reset = false;
            }
            if (Math.Abs(value.Mid() - _startValue.Value) > _maxTotalVol)
            {
                WriteBlockReason(updateTime, string.Format("stopped trading due to vol > {0}", _maxTotalVol));
                _stopTrading = true;
            }
            if (_tradingStart > DateTime.MinValue)
            {
                if ((decimal)(updateTime - _tradingStart).TotalMilliseconds > _timeFrameStopLossMn * 60000m)
                {
                    _rsi_loss_reset = false;
                    return close(_id + string.Format(" close event due to timeout, AssetStart = {0}, Value = {1}", _tradingStartValue, value.Bid), ref tradingOrder);
                }
                else
                {
                    int idxStopLoss = -1;
                    decimal ratio = 0m;
                    foreach (var interval in stopLossTimes)
                    {
                        idxStopLoss++;
                        if (interval.IsInside((int)(updateTime - _tradingStart).TotalMinutes))
                        {
                            ratio = interval.Ratio((decimal)(updateTime - _tradingStart).TotalMinutes);
                            break;
                        }
                    }
                    if (_signalCode == SIGNAL_CODE.BUY)
                    {
                        var assetMin = rsi.MinAsset((int)(updateTime - _tradingStart).TotalMinutes + 1);
                        var stopWin = stopWinValues[idxStopLoss].Value(ratio);
                        var stopBigLoss = bigStopLossValues[idxStopLoss].Value(ratio);
                        if (value.Bid >= _tradingStartValue + stopWin && curRsi.Bid >= getBuyThreshold() + 10)
                            return close(_id + string.Format(" close event due to stop win. SELL AssetStart = {0}, LastValue = {1}, StopWin = {2}", _tradingStartValue, value.Bid, stopWin), ref tradingOrder);
                        else if (value.Bid - assetMin >= stopWin && curRsi.Bid >= getBuyThreshold() + 10)
                        {
                            if (value.Bid > _tradingStartValue)
                                return close(_id + string.Format(" close event due to stop win. SELL AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Bid), ref tradingOrder);
                            else
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss. SELL AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Bid), ref tradingOrder);
                            }
                        }
                        else if (value.Bid - _tradingStartValue >= stopBigLoss)
                        {
                            _rsi_loss_reset = false;
                            return close(_id + string.Format(" close event due to stop loss. SELL AssetMin = {0}, LastValue = {1}, StopLoss = {2}", assetMin, value.Bid, stopBigLoss), ref tradingOrder);
                        }/*
                        else if (_trend.TimeSeries.Last().Bid <= _tradingTrend - 5m && curRsi.Bid >= getBuyThreshold() + 10)
                        {
                            if (value.Offer > _tradingStartValue)
                                return close(_id + string.Format(" close event due to stop win forced by trend. SELL AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            else
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss forced by trend. SELL AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            }
                        }*/
                        else
                        {
                            var stopLoss = stopLossValues[idxStopLoss].Value(ratio);
                            var bigStopLoss = bigStopLossValues[idxStopLoss].Value(ratio);
                            if ((_tradingStartValue - value.Bid >= stopLoss && curRsi.Bid >= getBuyThreshold() + 20) ||
                                (_tradingStartValue - value.Bid >= bigStopLoss))
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss. SELL AssetStart = {0}, LastValue = {1}, StopLoss = {2}", _tradingStartValue, value.Bid, stopLoss), ref tradingOrder);
                            }
                        }
                    }
                    else
                    {
                        var assetMax = rsi.MaxAsset((int)(updateTime - _tradingStart).TotalMinutes + 1);
                        var stopWin = stopWinValues[idxStopLoss].Value(ratio);
                        var stopBigLoss = bigStopLossValues[idxStopLoss].Value(ratio);
                        if (value.Offer <= _tradingStartValue - stopWin && curRsi.Bid <= getSellThreshold() - 10)
                            return close(_id + string.Format(" close event due to stop win. BUY AssetStart = {0}, LastValue = {1}, StopWin = {2}", _tradingStartValue, value.Offer, stopWin), ref tradingOrder);
                        else if (assetMax - value.Offer >= stopWin && curRsi.Bid <= getSellThreshold() - 10)
                        {
                            if (value.Offer < _tradingStartValue)
                                return close(_id + string.Format(" close event due to stop win. BUY AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            else
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss. BUY AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            }
                        }
                        else if (assetMax - value.Offer >= stopBigLoss)
                        {
                            _rsi_loss_reset = false;
                            return close(_id + string.Format(" close event due to stop loss. BUY AssetMax = {0}, LastValue = {1}, StopLoss = {2}", assetMax, value.Offer, stopBigLoss), ref tradingOrder);
                        }/*
                        else if (_trend.TimeSeries.Last().Bid >= _tradingTrend + 5m && curRsi.Bid <= getSellThreshold() - 10)
                        {
                            if (value.Offer < _tradingStartValue)
                                return close(_id + string.Format(" close event due to stop win forced by trend. BUY AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            else
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss forced by trend. BUY AssetStart = {0}, LastValue = {1}", _tradingStartValue, value.Offer), ref tradingOrder);
                            }
                        }*/
                        else
                        {
                            var stopLoss = stopLossValues[idxStopLoss].Value(ratio);
                            var bigStopLoss = bigStopLossValues[idxStopLoss].Value(ratio);
                            if ((value.Offer - _tradingStartValue >= stopLoss && curRsi.Bid <= getSellThreshold() - 20) ||
                                (value.Offer - _tradingStartValue >= bigStopLoss))
                            {
                                _rsi_loss_reset = false;
                                return close(_id + string.Format(" close event due to stop loss. BUY AssetStart = {0}, LastValue = {1}, StopLoss = {2}", _tradingStartValue, value.Offer, stopLoss), ref tradingOrder);
                            }
                        }
                    }

                }
            }
            else
            {
                if (!_rsi_loss_reset || _stopTrading || maxVal - minVal < _minVol || value.Offer - value.Bid > _maxSpread)
                    return false;
                /*
                foreach (var rsiRef in _rsiRefs)
                {
                    if (rsiRef.TimeSeries.Count > 0)
                    {
                        if (Math.Abs(curRsi.Mid() - 50m) < Math.Abs(rsiRef.TimeSeries.Last().Mid() - 50m))
                            return false;
                    }
                }*/
                string eventName = "";
                if (_calendar.IsNearEvent(_asset.Name, updateTime, ref eventName))
                {
                    WriteBlockReason(updateTime, "deal blocked by event: " + eventName);
                    _rsi_loss_reset = false;
                    return false;
                }
                var rsiMax = rsi.MaxRsi(_timeFramePeakMn);
                var rsiMin = rsi.MinRsi(_timeFramePeakMn);
                //var rsiLongMax = _rsiLong.MaxRsi(_timeFrameRsiLongMn);
                //var rsiLongMin = _rsiLong.MinRsi(_timeFrameRsiLongMn);
                var rsiAdjustment = Math.Max(0, rsi.MaxRsi(_timeFrameBottomMn) - getSellThreshold()) - Math.Max(0, getBuyThreshold() - rsi.MinRsi(_timeFrameBottomMn));
                var curEmaVeryShort = _emaVeryShort.TimeSeries.Last();
                var curEmaShort = _emaShort.TimeSeries.Last();
                var curEmaLong = _emaLong.TimeSeries.Last();
                var curVol = _wmvol.TimeSeries.Last().Bid;
                if (curVol < _maxWmVol)
                {
                    WriteBlockReason(updateTime, "deal blocked due to vol < " + _maxWmVol);
                    return false;
                }
                var curVolAvg = (_wmvol.Min() + _wmvol.Max()) / 2m;
                if (curVol < curVolAvg)
                {
                    WriteBlockReason(updateTime, "deal blocked due to vol < vol average");
                    return false;
                }                
                if ((curRsi.Bid >= getSellThreshold()) &&
                    //(curRsi.Bid - rsiAdjustment >= getSellThreshold() && curRsi.Bid >= (getSellThreshold() - 10))) && (rsiLongMax >= getSellThreshold() - 15m) &&
                    (curRsi.Bid > rsiMax - _startThreshold) && (curEmaVeryShort.Mid() + _maxShortMacDSpread > curEmaLong.Mid()) && 
                    (Math.Abs(curEmaShort.Mid() - curEmaLong.Mid()) < _maxLongMacDSpread))
                {
                    var curVolTrend = _volTrend.TimeSeries.Last().Bid;
                    if (curVolTrend > 0.5m || curVolTrend < 0m)
                    {
                        WriteBlockReason(updateTime, "deal blocked due to vol not within 0 < trend < 0.5");
                        return false;
                    }                    
                    if (!_volTrendTrend.IsMin(2, curVolTrend, 0.01m) || _volTrendTrend.IsMax(2, curVolTrend, 0.02m))
                    {
                        WriteBlockReason(updateTime, "sell event blocked due to vol trend not 1mn minimum");
                        return false;
                    }                   
                    tradingOrder = _onSell;
                    _signalCode = SIGNAL_CODE.SELL;
                    _tradingStart = updateTime;
                    _tradingStartValue = value.Bid;
                    _tradingTrend = _trend.TimeSeries.Last().Bid;
                    if (curRsi.Bid >= getSellThreshold())
                        Log.Instance.WriteEntry(_id + " sell event due to RSI >= getSellThreshold()");
                    //                    else if (Math.Abs(curRsi.Bid - rsiMax) < _startThreshold)
                    //                      Log.Instance.WriteEntry(_id + " sell event due highest RSI peak reached");
                    else
                        Log.Instance.WriteEntry(_id + " sell event due to adjusted RSI >= getSellThreshold()");
                    return true;
                }
                else if ((curRsi.Bid <= getBuyThreshold()) &&
                    //(curRsi.Bid + rsiAdjustment <= getBuyThreshold() && curRsi.Bid <= (getBuyThreshold() + 10))) && (rsiLongMin <= getBuyThreshold() + 15m) &&
                    (curRsi.Bid < rsiMin + _startThreshold) && (curEmaVeryShort.Mid() - _maxShortMacDSpread < curEmaLong.Mid()) && 
                    (Math.Abs(curEmaShort.Mid() - curEmaLong.Mid()) < _maxLongMacDSpread))
                {
                    var curVolTrend = _volTrend.TimeSeries.Last().Bid;
                    if (curVolTrend < -0.5m || curVolTrend > 0m)
                    {
                        WriteBlockReason(updateTime, "deal blocked due to vol not within -0.5 < trend < 0");
                        return false;
                    }
                    if (!_volTrendTrend.IsMax(2, curVolTrend, 0.01m) || _volTrendTrend.IsMin(2, curVolTrend, 0.02m))
                    {
                        WriteBlockReason(updateTime, "buy event blocked due to vol trend not 1mn maximum");
                        return false;
                    }
                    tradingOrder = _onBuy;
                    _signalCode = SIGNAL_CODE.BUY;
                    _tradingStart = updateTime;
                    _tradingStartValue = value.Offer;
                    _tradingTrend = _trend.TimeSeries.Last().Bid;
                    if (curRsi.Bid <= getBuyThreshold())
                        Log.Instance.WriteEntry(_id + " buy event due to RSI <= getBuyThreshold()");
                    //                   else if (Math.Abs(curRsi.Bid - rsiMin) < _startThreshold)
                    //                     Log.Instance.WriteEntry(_id + " buy event due lowest RSI peak reached");
                    else
                        Log.Instance.WriteEntry(_id + " buy event due to adjusted RSI <= getBuyThreshold()");
                    return true;
                }
                else
                {
                    _signalCode = SIGNAL_CODE.HOLD;
                    _tradingStart = DateTime.MinValue;
                    _tradingStartValue = 0m;
                }
            }
            return false;
        }
    }
}
