using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    class Heuristic
    {
        public static void Run(List<DateTime> dates, bool generate = false, bool generate_from_db = false, bool publish_to_db = false, bool use_uat_db = false, bool fullday = false)
        {
            TestEngine testEngine = new TestEngine("heuristic", dates, generate, generate_from_db, publish_to_db, use_uat_db, fullday);
            testEngine.Settings["TRADING_SIGNAL"] = "MacDCas_10_30_90_200_IX.D.DAX.DAILY.IP";
            testEngine.Settings["TIME_DECAY_FACTOR"] = "15";
            testEngine.Settings["ASSUMPTION_TREND"] = "BEAR";
            var models = new List<Model>();
            models.Add(new ModelMacDTest(new MarketData("DAX:IX.D.DAX.DAILY.IP"), 10, 30, 90));
            models.Add(new ModelMacDCascadeTest((ModelMacD)models[0]));
            models.Add(new ModelMoleTest((ModelMacD)models[0]));
            testEngine.Run(models);          
        }
    }
}
