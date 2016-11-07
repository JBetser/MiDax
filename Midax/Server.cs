using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;
using Midax;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using NLapack.Matrices;
using System.Runtime.InteropServices;

public class Server
{
    public class App : Ice.Application
    {
        Trader _trader;        

        public override int run(string[] args)
        {           
            try
            {
                if (args.Length != 0)
                    throw new ApplicationException("starting: too many arguments in application call.");

                Log.APPNAME = Midax.Properties.Settings.Default.APP_NAME;
                
                Dictionary<string, string> dicSettings = new Dictionary<string, string>();
                List<string> stockList = new List<string>();
                foreach (SettingsPropertyValue prop in Midax.Properties.Settings.Default.PropertyValues)
                {
                    if (prop.Name == "STOCKS")
                    {
                        string[] stockArray = new string[100];
                        ((StringCollection)prop.PropertyValue).CopyTo(stockArray, 0);
                        foreach (string stock in stockArray)
                        {
                            if (stock != null && stock != "")
                                stockList.Add(stock);
                        }
                    }
                    else
                        dicSettings.Add(prop.Name, (string)prop.PropertyValue);
                }
                Config.Settings = dicSettings;
            
                Ice.ObjectAdapter adapter = communicator().createObjectAdapter("MidaxIce");
                Ice.Properties properties = communicator().getProperties();
                Ice.Identity id = communicator().stringToIdentity(properties.getProperty("Identity"));

                Thread.Sleep(10000);

                /*
                List<string> rsiRefMappingGBP = new List<string> { dicSettings["FX_GBPEUR"], dicSettings["FX_GBPUSD"] };
                List<string> rsiRefMappingUSD = new List<string> { dicSettings["FX_USDJPY"], dicSettings["FX_EURUSD"] };
                List<decimal> volcoeffsGBP = new List<decimal> { 1m, 0.8m };
                List<decimal> volcoeffsUSD = new List<decimal> { 0.75m, 0.7m };*/

                var index = IceStreamingMarketData.Instance;
                var dax = new MarketData(dicSettings["INDEX_DAX"]);
                var gbpusd = new MarketData(dicSettings["FX_GBPUSD"]);
                var gbpeur = new MarketData(dicSettings["FX_GBPEUR"]);
                var eurusd = new MarketData(dicSettings["FX_EURUSD"]);
                var usdjpy = new MarketData(dicSettings["FX_USDJPY"]);
                var audusd = new MarketData(dicSettings["FX_AUDUSD"]);
                List<MarketData> otherIndices = new List<MarketData>();
                otherIndices.Add(new MarketData(dicSettings["INDEX_CAC"]));
                otherIndices.Add(new MarketData(dicSettings["INDEX_DOW"]));
                otherIndices.Add(gbpusd);
                otherIndices.Add(gbpeur);
                otherIndices.Add(eurusd);
                otherIndices.Add(usdjpy);
                otherIndices.Add(audusd);
                var models = new List<Model>();
                var macD_10_30_90_dax = new ModelMacD(dax, 10, 30, 90);
                var macD_10_30_90_gbpusd = new ModelMacD(gbpusd, 10, 30, 90);
                var macD_10_30_90_eurusd = new ModelMacD(eurusd, 10, 30, 90);
                var macD_10_30_90_usdjpy = new ModelMacD(usdjpy, 10, 30, 90);
                var macD_10_30_90_audusd = new ModelMacD(audusd, 10, 30, 90);
                decimal volcoeffGBPUSD = 0.85m;
                decimal volcoeffEURUSD = 0.7m;
                decimal volcoeffUSDJPY = 0.65m;
                decimal volcoeffAUDUSD = 0.6m;
                var fxmole_eur = new ModelFXMole(new List<MarketData> { eurusd, gbpusd }, macD_10_30_90_eurusd, volcoeffEURUSD);
                var fxmole_gbp = new ModelFXMole(new List<MarketData> { gbpusd, eurusd }, macD_10_30_90_gbpusd, volcoeffGBPUSD);
                var fxmole_jpy = new ModelFXMole(new List<MarketData> { usdjpy, eurusd }, macD_10_30_90_usdjpy, volcoeffUSDJPY);
                var fxmole_aud = new ModelFXMole(new List<MarketData> { audusd, eurusd }, macD_10_30_90_audusd, volcoeffAUDUSD);
                models.Add(macD_10_30_90_gbpusd);
                models.Add(macD_10_30_90_eurusd);
                models.Add(macD_10_30_90_usdjpy);
                models.Add(macD_10_30_90_audusd);
                models.Add(fxmole_eur);
                models.Add(fxmole_gbp);
                models.Add(fxmole_jpy);
                models.Add(fxmole_aud);
                models.Add(macD_10_30_90_dax);
                models.Add(new ModelANN("WMA_5_2", macD_10_30_90_dax, null, null, otherIndices));
                _trader = new Trader(models, communicator().shutdown); 
                _trader.Init(Config.GetNow);

                adapter.add(new MidaxIceI(_trader, properties.getProperty("Ice.ProgramName")), id);
                adapter.activate();
                communicator().waitForShutdown();
            }
            catch (SEHException exc)
            {
                Log.Instance.WriteEntry("Midax server interop error: " + exc.ToString() + ", Error code: " + exc.ErrorCode, EventLogEntryType.Error);
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry("Midax server error: " + exc.ToString(), EventLogEntryType.Error);
            }

            return 0;
        }             
    }

    static public int Main(string[] args)
    {
        App app = new App();
        return app.main(args);
    }
}

