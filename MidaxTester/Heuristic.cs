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
            testEngine.Settings["INDEX_DAX"] = "DAX:IX.D.DAX.DAILY.IP";
            testEngine.Settings["INDEX_CAC"] = "CAC:IX.D.CAC.DAILY.IP";
            testEngine.Settings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            testEngine.Settings["FX_GBPEUR"] = "GBPEUR:CS.D.GBPEUR.TODAY.IP";

            var models = new List<Model>();            
            var dow = new MarketData(testEngine.Settings["INDEX_ICEDOW"], testEngine.Settings["INDEX_DOW"]);
            var dowIG = new MarketData(testEngine.Settings["INDEX_DOW"]);
            var dax = new MarketData(testEngine.Settings["INDEX_DAX"]);
            models.Add(new ModelMacDVTest(dow, 10, 30, 90, dowIG));
            models.Add(new ModelMacDTest(dax, 10, 30, 90));
            models.Add(new ModelMacDCascadeTest((ModelMacD)models[1]));
            models.Add(new ModelMoleTest((ModelMacD)models[1]));
            testEngine.Run(models);          
        }
    }
}
