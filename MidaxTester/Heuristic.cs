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
            testEngine.Settings["TRADING_SIGNAL"] = "FXMole_1_14_CS.D.GBPUSD.TODAY.IP,FXMole_1_14_CS.D.GBPEUR.TODAY.IP";
            testEngine.Settings["TIME_DECAY_FACTOR"] = "3";
            testEngine.Settings["ASSUMPTION_TREND"] = "BEAR";
            testEngine.Settings["INDEX_ICEDOW"] = "DOW:IceConnection.DJI";
            testEngine.Settings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            testEngine.Settings["INDEX_DAX"] = "DAX:IX.D.DAX.DAILY.IP";
            testEngine.Settings["INDEX_CAC"] = "CAC:IX.D.CAC.DAILY.IP";
            testEngine.Settings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            testEngine.Settings["FX_GBPEUR"] = "GBPEUR:CS.D.GBPEUR.TODAY.IP";
            List<string> rsiRefMapping = new List<string> { "CS.D.GBPEUR.TODAY.IP", "CS.D.GBPUSD.TODAY.IP" };
            List<decimal> volcoeffs = new List<decimal> { 1m, 0.8m };

            var models = new List<Model>();            
            var gbpusd = new MarketData(testEngine.Settings["FX_GBPUSD"]);
            var gbpeur = new MarketData(testEngine.Settings["FX_GBPEUR"]);
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(gbpusd);
            mktData.Add(gbpeur);
            var macD_10_30_90_gbpusd = new ModelMacD(gbpusd, 10, 30, 90);
            var macD_10_30_90_gbpeur = new ModelMacD(gbpeur, 10, 30, 90);
            //var dax = new MarketData(testEngine.Settings["INDEX_DAX"]);
            models.Add(macD_10_30_90_gbpusd);
            models.Add(macD_10_30_90_gbpeur);
            models.Add(new ModelFXMole(mktData, new List<ModelMacD> { macD_10_30_90_gbpusd, macD_10_30_90_gbpeur }, rsiRefMapping, volcoeffs));
            //models.Add(new ModelFXMole(dax));
            //models.Add(new ModelMacD(gbpusd, 2, 10, 30));
            testEngine.Run(models);          
        }
    }
}
