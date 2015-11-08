using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLapack;
using NLapack.Matrices;

namespace MidaxLib
{
    public class IndicatorLinearRegression : Indicator
    {
        TimeSpan _interval;

        public IndicatorLinearRegression(MarketData mktData, TimeSpan interval)
            : base("LR_" + interval.Hours + "_" + interval.Minutes + "_" + interval.Seconds + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _interval = interval;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.Count > 1)
            {
                decimal? coeff = linearCoeff(updateTime);
                if (coeff != null)
                {
                    Price price = new Price(coeff.Value);
                    base.OnUpdate(mktData, updateTime, price);
                    Publish(updateTime, price);
                }
            }
        }

        decimal? linearCoeff(DateTime updateTime)
        {
            List<KeyValuePair<DateTime, Price>> values = _mktData[0].TimeSeries.Values(updateTime, _interval);
            if (values == null)
                return null;
            if (values.Count < 2)
                return null;
            var V = new NRealMatrix(values.Count, 2);
            var Vt = new NRealMatrix(values.Count, 2);
            var Y = new NRealMatrix(values.Count, 1);
            int idxRow = 0;
            DateTime startTime = updateTime - _interval;
            foreach (var keyVal in values)
            {
                V[idxRow, 0] = 1;
                V[idxRow, 1] = keyVal.Key.TimeOfDay.TotalSeconds - startTime.TimeOfDay.TotalSeconds;
                Vt[idxRow, 0] = V[idxRow, 0];
                Vt[idxRow, 1] = V[idxRow, 1];
                Y[idxRow, 0] = Convert.ToDouble(keyVal.Value.Mid());
                idxRow += 1;
            }
            Vt.Transpose();
            var VtV = Vt * V;
            var VtY = Vt * Y;

            var X = new NRealMatrix(2, 1);
            //solve VtV * X = VtY
            LapackLib.Instance.SolveSle(VtV, VtY, X);
            if (X[1, 0] == double.NaN)
            {
                Log.Instance.WriteEntry("Invalid linear regression " + _mktData[0].Name, EventLogEntryType.Error);
                return null;
            }
            return Convert.ToDecimal(X[1, 0]);
        }
    }
}
