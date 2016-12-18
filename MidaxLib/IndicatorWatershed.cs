using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    class IndicatorWatershed : Indicator
    {
        int _nbPools;
        int _resolutionSeconds;
        DateTime _nextWatershedTime = DateTime.MinValue;
        List<Pool> _pools = new List<Pool>();
        Pool _curPool = null;
        SIGNAL_CODE _trend = SIGNAL_CODE.SELL;
        IndicatorRSI _rsi;

        public IndicatorWatershed(MarketData mktData, int resolutionMinutes, int nbPools, IndicatorRSI rsi)
            : base("Water_" + resolutionMinutes + "_" + nbPools + "_", new List<MarketData> { mktData })
        {
            _resolutionSeconds = resolutionMinutes * 60;
            _nbPools = nbPools;
            _rsi = rsi;
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trend = Config.Settings["ASSUMPTION_TREND"] == "BEAR" ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
        }

        protected bool CalcWatershed(MarketData mktData, DateTime updateTime, Price value)
        {
            DateTime startTime = updateTime.AddSeconds(-_resolutionSeconds);
            if (SignalStock.TimeSeries.StartTime() > startTime)
                return false;
            bool updatePools = false;
            if (updateTime > _nextWatershedTime)
            {
                _nextWatershedTime = (_nextWatershedTime == DateTime.MinValue ? updateTime : _nextWatershedTime).AddSeconds(_resolutionSeconds);
                updatePools = true;
            }
            if (_curPool == null)
            {
                _curPool = new Pool(_trend, updateTime);
                _pools.Add(_curPool);
            }
            if (_curPool.Update(updateTime, value, _rsi.TimeSeries.Last()))
            {
                if (updatePools)
                {
                    _curPool = new Pool(_trend, updateTime);
                    _pools.Add(_curPool);
                }
            }
            if (_pools.Count > _nbPools)
                _pools.RemoveAt(0);
            return updatePools;
        }

        void publishPool(Pool pool, DateTime updateTime, int idxPool)
        {
            string saveId = _id;
            var lastCompletePool = _pools.Last().ReachedExtremum ? _pools.Last() : _pools[_pools.Count - 2];
            decimal valueRange = lastCompletePool.LastValue - _pools.First().StartValue;
            TimeSpan timeRange = lastCompletePool.LastTime - _pools.First().StartTime;
            _id = saveId + "depth" + idxPool + "_" + SignalStock.Id;
            var val = Math.Abs(valueRange) < 0.1m ? 0m : pool.Depth / valueRange;
            Publish(updateTime, val);
            _id = saveId + "valuediff" + idxPool + "_" + SignalStock.Id;
            val = Math.Abs(valueRange) < 0.1m ? 0m : (pool.LastValue - pool.StartValue) / valueRange;
            Publish(updateTime, val);
            _id = saveId + "timediff" + idxPool + "_" + SignalStock.Id;
            val = Math.Abs(timeRange.TotalMilliseconds) < 10.0 ? 0m : (decimal)((pool.LastTime - pool.StartTime).TotalMilliseconds / timeRange.TotalMilliseconds);
            Publish(updateTime, val);
            _id = saveId + "rsi" + idxPool + "_" + SignalStock.Id;
            val = pool.LastRsi / 100m;
            Publish(updateTime, val);
            _id = saveId;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            bool updateAllPools = CalcWatershed(mktData, updateTime, value);
            if (updateAllPools && _pools.Count == _nbPools)
            {
                int idxPool = 0;
                foreach (var pool in _pools)
                {
                    if (pool.ReachedExtremum)
                        publishPool(pool, _nextWatershedTime, idxPool);
                    idxPool++;
                }
            }
        }
    }

    class Pool
    {
        public decimal Depth = 0m;
        public DateTime StartTime;
        public decimal StartValue = decimal.MinValue;
        public DateTime ExtremumTime;
        public decimal ExtremumValue = 0m;
        public DateTime LastTime = DateTime.MinValue;
        public decimal LastValue = decimal.MinValue;
        public decimal LastRsi = 0m;
        public SIGNAL_CODE GlobalTrend = SIGNAL_CODE.UNKNOWN;
        public SIGNAL_CODE PoolTrend = SIGNAL_CODE.UNKNOWN;
        public bool ReachedExtremum = false;

        public Pool(SIGNAL_CODE globalTrend, DateTime startTime)
        {
            GlobalTrend = globalTrend;
            StartTime = startTime;
            ExtremumValue = GlobalTrend == SIGNAL_CODE.BUY ? decimal.MinValue : decimal.MaxValue;
        }

        public bool Update(DateTime updateTime, Price value, Price rsi)
        {           
            var curValue = value.Mid();
            if (StartValue == decimal.MinValue)
                StartValue = curValue;
            if (GlobalTrend == SIGNAL_CODE.BUY)
            {
                if (ReachedExtremum)
                {
                    if (curValue > LastValue)
                    {
                        PoolTrend = LastValue < StartValue ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
                        Depth = LastValue < StartValue ? ExtremumValue - StartValue : ExtremumValue - LastValue;
                        return true;
                    }
                }
                else
                {
                    if (curValue > ExtremumValue)
                    {
                        ExtremumValue = curValue;
                        ExtremumTime = updateTime;
                    }
                    else if (curValue < ExtremumValue)
                    {
                        ReachedExtremum = true;
                    }
                }
            }
            else
            {
                if (ReachedExtremum)
                {
                    if (curValue < LastValue)
                    {
                        PoolTrend = LastValue < StartValue ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
                        Depth = LastValue < StartValue ? LastValue - ExtremumValue : StartValue - ExtremumValue;
                        return true;
                    }
                }
                else
                {
                    if (curValue < ExtremumValue)
                    {
                        ExtremumValue = curValue;
                        ExtremumTime = updateTime;
                    }
                    else if (curValue > ExtremumValue)
                    {
                        ReachedExtremum = true;
                    }
                }
            }
            LastTime = updateTime;
            LastValue = curValue;
            LastRsi = rsi.Bid;
            return false;
        }
    }
}
