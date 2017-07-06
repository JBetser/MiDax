using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    public class ANN
    {
        public static void Run(List<DateTime> dates, bool generate = false, bool generate_from_db = false, bool publish_to_db = false, bool use_uat_db = false, bool fullday = false)
        {
            TestEngine testEngine = new TestEngine("ANN", dates, generate, generate_from_db, publish_to_db, use_uat_db, fullday);
            testEngine.Settings["TRADING_SIGNAL"] = "ANN_FX_5_2_1_CS.D.EURUSD.TODAY.IP";
            testEngine.Settings["TIME_GMT_CALENDAR"] = "1";
            var models = new List<Model>();
            var index = new MarketData("EURUSD:CS.D.EURUSD.TODAY.IP");
            models.Add(new ModelMacDTest(index, 10, 30, 90));
            List<MarketData> otherIndices = new List<MarketData>();
            otherIndices.Add(index);
            models.Add(new ModelANN("FX_5_2", (ModelMacD)models[0], null, null, otherIndices));
            testEngine.Run(models);
        }
    }
}
