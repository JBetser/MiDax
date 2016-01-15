using System;
using System.Collections.Generic;
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
        public IndicatorLevel(MarketData mktData, string indicatorid)
            : base(indicatorid + "_" + mktData.Id, new List<MarketData> { mktData })
        {
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        public virtual void Publish(DateTime updateTime)
        {
        }
    }

    public class IndicatorLevelMean : IndicatorWMA, ILevelPublisher
    {
        // Whole day average
        public IndicatorLevelMean(MarketData mktData)
            : base("WMA_1D_" + mktData.Id, mktData, 0)
        {
            TimeSpan timeDiff = (DateTime.Parse(Config.Settings["PUBLISHING_STOP_TIME"]) - DateTime.Parse(Config.Settings["PUBLISHING_START_TIME"]));
            _periodSeconds = (timeDiff.Hours * 60 + timeDiff.Minutes) * 60 + timeDiff.Seconds;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }

        void ILevelPublisher.Publish(DateTime updateTime)
        {
            Price avg = Average(_mktData[0], updateTime, true);
            Publish(updateTime, avg.MidPrice());
        }

        public Price Average()
        {
            return Average(_mktData[0], Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]), true);
        }
    }

    public class IndicatorLevelPivot : IndicatorLevel
    {
        public IndicatorLevelPivot(MarketData mktData)
            : base(mktData, "LVLPivot"){}

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.Pivot));
        }
    }

    public class IndicatorLevelR1 : IndicatorLevel
    {
        public IndicatorLevelR1(MarketData mktData)
            : base(mktData, "LVLR1") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.R1));
        }
    }

    public class IndicatorLevelR2 : IndicatorLevel
    {
        public IndicatorLevelR2(MarketData mktData)
            : base(mktData, "LVLR2") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.R2));
        }
    }

    public class IndicatorLevelR3 : IndicatorLevel
    {
        public IndicatorLevelR3(MarketData mktData)
            : base(mktData, "LVLR3") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.R3));
        }
    }

    public class IndicatorLevelS1 : IndicatorLevel
    {
        public IndicatorLevelS1(MarketData mktData)
            : base(mktData, "LVLS1") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.S1));
        }
    }

    public class IndicatorLevelS2 : IndicatorLevel
    {
        public IndicatorLevelS2(MarketData mktData)
            : base(mktData, "LVLS2") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.S2));
        }
    }

    public class IndicatorLevelS3 : IndicatorLevel
    {
        public IndicatorLevelS3(MarketData mktData)
            : base(mktData, "LVLS3") { }

        public override void Publish(DateTime updateTime)
        {
            Publish(updateTime, new Price(((MarketData)_mktData[0]).Levels.Value.S3));
        }
    }
}
