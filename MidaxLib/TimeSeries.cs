using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class TimeSeries
    {
        const int MAX_RECORD_TIME_HOURS = 2;
        const int TIMESERIES_INTERVAL_MINUTES = 10;

        List<SortedDictionary<DateTime, Price>> _series = new List<SortedDictionary<DateTime, Price>>();

        public void Add(DateTime updateTime, Price price)
        {
            if (_series.Count == 0)
                _series.Add(new SortedDictionary<DateTime, Price>());
            else if ((updateTime - _series[_series.Count - 1].Keys.ElementAt(_series[_series.Count - 1].Count - 1)).Minutes > TIMESERIES_INTERVAL_MINUTES)
            {
                deleteOldData(updateTime);
                _series.Add(new SortedDictionary<DateTime, Price>());
            }
            _series[_series.Count - 1][updateTime] = price;            
        }

        public KeyValuePair<DateTime, Price>? Value(DateTime time)
        {
            if (_series.Count == 0)
                return null;
            if (time < _series[0].Keys.ElementAt(0))
                return null;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (time < _series[idx].Keys.ElementAt(0))
                {
                    if (idx == 0)
                        return null;
                    return _series[idx - 1].Last(keyValue => keyValue.Key <= time);
                }
                else
                {
                    if (time <= _series[idx].Keys.ElementAt(_series[idx].Keys.Count - 1))
                        return _series[idx].Last(keyValue => keyValue.Key <= time);
                }
            }
            return null;
        }

        public KeyValuePair<DateTime, Price>? this[DateTime time]
        {
            get { return Value(time); }
        }

        public int Count
        {
            get { return (from SortedDictionary<DateTime, Price> dic in _series select dic.Count).Sum(); }
        }

        void deleteOldData(DateTime updateTime)
        {
            while (_series.Count > 0)
            {
                if ((updateTime - _series[0].Keys.ElementAt(_series[0].Count - 1)).Hours > MAX_RECORD_TIME_HOURS)
                    _series.RemoveAt(0);
                else
                    break;
            }
        }
    }
}
