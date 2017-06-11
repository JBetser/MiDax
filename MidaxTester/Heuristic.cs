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
            testEngine.Settings["TRADING_SIGNAL"] = "Rob_60_48_20_15_IX.D.DAX.DAILY.IP,Rob_60_48_20_15_IX.D.FTSE.DAILY.IP,Rob_60_48_20_15_CS.D.EURUSD.TODAY.IP,Rob_60_48_20_15_CS.D.GBPUSD.TODAY.IP,Rob_60_48_20_15_CS.D.USCSI.TODAY.IP";
            testEngine.Settings["TIME_GMT"] = "1";
            testEngine.Settings["TIME_DECAY_FACTOR"] = "3";
            testEngine.Settings["ASSUMPTION_TREND"] = "BEAR";
            testEngine.Settings["INDEX_ICEDOW"] = "DOW:IceConnection.DJI";
            testEngine.Settings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            testEngine.Settings["INDEX_DAX"] = "DAX:IX.D.DAX.DAILY.IP";
            testEngine.Settings["INDEX_CAC"] = "CAC:IX.D.CAC.DAILY.IP";
            testEngine.Settings["INDEX_FTSE"] = "FTSE:IX.D.FTSE.DAILY.IP";
            testEngine.Settings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            testEngine.Settings["FX_EURUSD"] = "EURUSD:CS.D.EURUSD.TODAY.IP";
            testEngine.Settings["FX_USDJPY"] = "USDJPY:CS.D.USDJPY.TODAY.IP";
            testEngine.Settings["FX_AUDUSD"] = "AUDUSD:CS.D.AUDUSD.TODAY.IP";
            testEngine.Settings["COM_SILVER"] = "SIL:CS.D.USCSI.TODAY.IP";
            
            //List<string> rsiRefMappingJPYGBP = new List<string> { testEngine.Settings["FX_GBPEUR"], testEngine.Settings["FX_USDJPY"] };
            //List<string> rsiRefMappingUSD = new List<string> { testEngine.Settings["FX_GBPUSD"], testEngine.Settings["FX_EURUSD"] };
            //List<decimal> volcoeffsJPYGBP = new List<decimal> { 0.7m, 0.8m };
            //List<decimal> volcoeffsUSD = new List<decimal> { 0.75m, 1.0m, 0.8m };

            var index = IceStreamingMarketData.Instance;
            var dax = new MarketData(testEngine.Settings["INDEX_DAX"]);
            var dow = new MarketData(testEngine.Settings["INDEX_DOW"]);
            var cac = new MarketData(testEngine.Settings["INDEX_CAC"]);
            var ftse = new MarketData(testEngine.Settings["INDEX_FTSE"]);
            var gbpusd = new MarketData(testEngine.Settings["FX_GBPUSD"]);
            var eurusd = new MarketData(testEngine.Settings["FX_EURUSD"]);
            var silver = new MarketData(testEngine.Settings["COM_SILVER"]);
            var models = new List<Model>();
            var robinhood_gbpusd = new ModelRobinHood(gbpusd);
            var robinhood_eurusd = new ModelRobinHood(eurusd);
            var robinhood_dax = new ModelRobinHood(dax);
            //var robinhood_ftse = new ModelRobinHood(ftse);
            //var robinhood_sil = new ModelRobinHood(silver);

            models.Add(robinhood_gbpusd);
            models.Add(robinhood_eurusd);
            models.Add(robinhood_dax);
            //models.Add(robinhood_ftse);
            //models.Add(robinhood_sil);
            testEngine.Run(models);          
        }
    }
}
