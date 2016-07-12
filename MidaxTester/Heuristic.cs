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
            testEngine.Settings["TRADING_SIGNAL"] = "MacDCas_10_30_90_200_IceConnection.DJI";
            testEngine.Settings["TIME_DECAY_FACTOR"] = "3";
            testEngine.Settings["ASSUMPTION_TREND"] = "BEAR";
            testEngine.Settings["INDEX_ICEDOW"] = "DOW:IceConnection.DJI";
            testEngine.Settings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            var models = new List<Model>();
            var dow = new MarketData(testEngine.Settings["INDEX_DOW"]);
            var dowNSYE = new MarketData(testEngine.Settings["INDEX_ICEDOW"], testEngine.Settings["INDEX_DOW"]);
            models.Add(new ModelMacDTest(dow, 10, 30, 90));
            models.Add(new ModelMacDVTest(dowNSYE, 10, 30, 90, dow));
            models.Add(new ModelMoleTest((ModelMacD)models[0]));
            models.Add(new ModelMacDCascadeTest((ModelMacDV)models[1]));
            testEngine.Run(models);          
        }
    }
}
