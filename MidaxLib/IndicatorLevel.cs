using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public interface ILevelPublisher
    {
        void Publish(DateTime updateTime);
    }

    public abstract class IndicatorLevel : Indicator, ILevelPublisher
    {
        protected MarketData _levelMktData = null;

        public IndicatorLevel(MarketData mktData, string indicatorid)
            : base(indicatorid + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _levelMktData = mktData;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        public virtual void Publish(DateTime updateTime)
        {
            if (_levelMktData.Levels.HasValue)
                publish(updateTime);
            else
                Log.Instance.WriteEntry("Cannot publish level indicator " + _id + ": value is unvavailable", EventLogEntryType.Error);
        }

        public virtual void publish(DateTime updateTime)
        { 
        }
    }

    public class IndicatorLow : IndicatorLevel
    {
        protected decimal _low = decimal.MaxValue;

        public IndicatorLow(MarketData mktData)
            : base(mktData, "Low")
        {            
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (_low > value.Bid)
                _low = value.Bid;
        }

        public override void Publish(DateTime updateTime)
        {
            if (_low == decimal.MaxValue && _levelMktData.Levels.HasValue)
                _low = _levelMktData.Levels.Value.Low;
            Publish(updateTime, _low);
        }
    }

    public class IndicatorHigh : IndicatorLevel
    {
        protected decimal _high = decimal.MinValue;

        public IndicatorHigh(MarketData mktData)
            : base(mktData, "High")
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (_high < value.Offer)
                _high = value.Offer;
        }

        public override void Publish(DateTime updateTime)
        {
            if (_high == decimal.MinValue && _levelMktData.Levels.HasValue)
                _high = _levelMktData.Levels.Value.High;
            Publish(updateTime, _high);
        }
    }

    public class IndicatorCloseBid : IndicatorLevel
    {
        protected decimal _closeBid = decimal.MinValue;

        public IndicatorCloseBid(MarketData mktData)
            : base(mktData, "CloseBid")
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            _closeBid = value.Bid;
        }

        public override void Publish(DateTime updateTime)
        {
            if (_closeBid == decimal.MinValue && _levelMktData.Levels.HasValue)
                _closeBid = _levelMktData.Levels.Value.CloseBid;
            Publish(updateTime, _closeBid);
        }
    }

    public class IndicatorCloseOffer : IndicatorLevel
    {
        protected decimal _closeOffer = decimal.MinValue;

        public IndicatorCloseOffer(MarketData mktData)
            : base(mktData, "CloseOffer")
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            _closeOffer = value.Offer;
        }

        public override void Publish(DateTime updateTime)
        {
            if (_closeOffer == decimal.MinValue && _levelMktData.Levels.HasValue)
                _closeOffer = _levelMktData.Levels.Value.CloseOffer;
            Publish(updateTime, _closeOffer);
        }
    }

    public class IndicatorLevelMean : IndicatorWMA, ILevelPublisher
    {
        // Whole day average
        public IndicatorLevelMean(MarketData mktData)
            : base("WMA_1D_" + mktData.Id, mktData, 0)
        {
            TimeSpan timeDiff = (Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]) - Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]));
            _subPeriodSeconds = (timeDiff.Hours * 60 + timeDiff.Minutes) * 60 + timeDiff.Seconds;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        void ILevelPublisher.Publish(DateTime updateTime)
        {
            if (_mktData[0].TimeSeries.Count == 0 || _mktData[0].TimeSeries.TotalMinutes(updateTime) < 240)
            {
                Log.Instance.WriteEntry("Cannot publish level mean indicator: no market data available", EventLogEntryType.Warning);
                return;
            }
            Price avg = Average(updateTime);
            Publish(updateTime, avg.MidPrice());            
        }

        public Price Average()
        {
            return Average(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
        }
    }

    public class IndicatorLevelPivot : IndicatorLevel
    {
        public IndicatorLevelPivot(MarketData mktData)
            : base(mktData, "LVLPivot"){}

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.Pivot));
        }
    }            

    public class IndicatorLevelR1 : IndicatorLevel
    {
        public IndicatorLevelR1(MarketData mktData)
            : base(mktData, "LVLR1") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.R1));
        }
    }

    public class IndicatorLevelR2 : IndicatorLevel
    {
        public IndicatorLevelR2(MarketData mktData)
            : base(mktData, "LVLR2") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.R2));
        }
    }

    public class IndicatorLevelR3 : IndicatorLevel
    {
        public IndicatorLevelR3(MarketData mktData)
            : base(mktData, "LVLR3") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.R3));
        }
    }

    public class IndicatorLevelS1 : IndicatorLevel
    {
        public IndicatorLevelS1(MarketData mktData)
            : base(mktData, "LVLS1") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.S1));
        }
    }

    public class IndicatorLevelS2 : IndicatorLevel
    {
        public IndicatorLevelS2(MarketData mktData)
            : base(mktData, "LVLS2") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.S2));
        }
    }

    public class IndicatorLevelS3 : IndicatorLevel
    {
        public IndicatorLevelS3(MarketData mktData)
            : base(mktData, "LVLS3") { }

        public override void publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(_levelMktData.Levels.Value.S3));
        }
    }

    public class IndicatorNearestLevel : Indicator
    {
        public IndicatorNearestLevel(MarketData mktData)
            : base("NearestLevel_" + mktData.Id, new List<MarketData> { mktData }) { }

        public static decimal GetNearestLevel(decimal midPrice, MarketLevels mktLevels)
        {
            decimal referenceLevel = 0m;
            var diff = decimal.MaxValue;
            if (Math.Abs(midPrice - mktLevels.R3) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R3);
                referenceLevel = mktLevels.R3;
            }
            if (Math.Abs(midPrice - mktLevels.R2) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R2);
                referenceLevel = mktLevels.R2;
            }
            if (Math.Abs(midPrice - mktLevels.R1) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.R1);
                referenceLevel = mktLevels.R1;
            }
            if (Math.Abs(midPrice - mktLevels.Pivot) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.Pivot);
                referenceLevel = mktLevels.Pivot;
            }
            if (Math.Abs(midPrice - mktLevels.S1) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S1);
                referenceLevel = mktLevels.S1;
            }
            if (Math.Abs(midPrice - mktLevels.S2) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S2);
                referenceLevel = mktLevels.S2;
            }
            if (Math.Abs(midPrice - mktLevels.S3) < diff)
            {
                diff = Math.Abs(midPrice - mktLevels.S3);
                referenceLevel = mktLevels.S3;
            }
            return referenceLevel;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (!mktData.Levels.HasValue)
                return;
            Publish(updateTime, new Price(GetNearestLevel(value.Mid(), mktData.Levels.Value)));
        }
    }
}
