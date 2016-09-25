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
            testEngine.Settings["TRADING_SIGNAL"] = "FXMole_1_14_CS.D.EURUSD.TODAY.IP,FXMole_1_14_CS.D.GBPUSD.TODAY.IP,FXMole_1_14_CS.D.USDJPY.TODAY.IP,FXMole_1_14_CS.D.AUDUSD.TODAY.IP";
            testEngine.Settings["TIME_GMT"] = "-4";
            testEngine.Settings["TIME_DECAY_FACTOR"] = "3";
            testEngine.Settings["ASSUMPTION_TREND"] = "BEAR";
            testEngine.Settings["INDEX_ICEDOW"] = "DOW:IceConnection.DJI";
            testEngine.Settings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            testEngine.Settings["INDEX_DAX"] = "DAX:IX.D.DAX.DAILY.IP";
            testEngine.Settings["INDEX_CAC"] = "CAC:IX.D.CAC.DAILY.IP";
            testEngine.Settings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            testEngine.Settings["FX_EURUSD"] = "EURUSD:CS.D.EURUSD.TODAY.IP";
            testEngine.Settings["FX_USDJPY"] = "USDJPY:CS.D.USDJPY.TODAY.IP";
            testEngine.Settings["FX_AUDUSD"] = "AUDUSD:CS.D.AUDUSD.TODAY.IP";
            
            //List<string> rsiRefMappingJPYGBP = new List<string> { testEngine.Settings["FX_GBPEUR"], testEngine.Settings["FX_USDJPY"] };
            //List<string> rsiRefMappingUSD = new List<string> { testEngine.Settings["FX_GBPUSD"], testEngine.Settings["FX_EURUSD"] };
            //List<decimal> volcoeffsJPYGBP = new List<decimal> { 0.7m, 0.8m };
            //List<decimal> volcoeffsUSD = new List<decimal> { 0.75m, 1.0m, 0.8m };

            var index = IceStreamingMarketData.Instance;
            var gbpusd = new MarketData(testEngine.Settings["FX_GBPUSD"]);
            var eurusd = new MarketData(testEngine.Settings["FX_EURUSD"]);
            var usdjpy = new MarketData(testEngine.Settings["FX_USDJPY"]);
            var audusd = new MarketData(testEngine.Settings["FX_AUDUSD"]);
            var models = new List<Model>();
            var macD_10_30_90_gbpusd = new ModelMacD(gbpusd, 10, 30, 90);
            var macD_10_30_90_eurusd = new ModelMacD(eurusd, 10, 30, 90);
            var macD_10_30_90_usdjpy = new ModelMacD(usdjpy, 10, 30, 90);
            var macD_10_30_90_audusd = new ModelMacD(audusd, 10, 30, 90);
            decimal volcoeffEURUSD = 0.7m;
            decimal volcoeffGBPUSD = 0.85m;
            decimal volcoeffUSDJPY = 0.65m;
            decimal volcoeffAUDUSD = 0.6m;
            var fxmole_eurusd = new ModelFXMole(new List<MarketData> { eurusd, gbpusd }, macD_10_30_90_eurusd, volcoeffEURUSD);
            var fxmole_gbpusd = new ModelFXMole(new List<MarketData> { gbpusd, eurusd }, macD_10_30_90_gbpusd, volcoeffGBPUSD);
            var fxmole_usdjpy = new ModelFXMole(new List<MarketData> { usdjpy, eurusd }, macD_10_30_90_usdjpy, volcoeffUSDJPY);
            var fxmole_audusd = new ModelFXMole(new List<MarketData> { audusd, eurusd }, macD_10_30_90_audusd, volcoeffAUDUSD);
            models.Add(macD_10_30_90_gbpusd);
            models.Add(macD_10_30_90_eurusd);
            models.Add(macD_10_30_90_usdjpy);
            models.Add(macD_10_30_90_audusd);
            models.Add(fxmole_gbpusd);
            models.Add(fxmole_eurusd);
            models.Add(fxmole_usdjpy);
            models.Add(fxmole_audusd);
            testEngine.Run(models);          
        }
    }
}
