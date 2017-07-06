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

                //Thread.Sleep(10000);

                /*
                List<string> rsiRefMappingGBP = new List<string> { dicSettings["FX_GBPEUR"], dicSettings["FX_GBPUSD"] };
                List<string> rsiRefMappingUSD = new List<string> { dicSettings["FX_USDJPY"], dicSettings["FX_EURUSD"] };
                List<decimal> volcoeffsGBP = new List<decimal> { 1m, 0.8m };
                List<decimal> volcoeffsUSD = new List<decimal> { 0.75m, 0.7m };*/

                var index = IceStreamingMarketData.Instance;
                var dax = new MarketData(dicSettings["INDEX_DAX"]);
                var dow = new MarketData(dicSettings["INDEX_DOW"]);
                var cac = new MarketData(dicSettings["INDEX_CAC"]);
                var ftse = new MarketData(dicSettings["INDEX_FTSE"]);
                var icedow = new MarketData(dicSettings["INDEX_ICEDOW"]);
                var gbpusd = new MarketData(dicSettings["FX_GBPUSD"]);
                var eurusd = new MarketData(dicSettings["FX_EURUSD"]);
                var btcusd = new MarketData(dicSettings["FX_BTCUSD"]);
                var silver = new MarketData(dicSettings["COM_SILVER"]);
                /*
                List<MarketData> otherIndices = new List<MarketData>();
                otherIndices.Add(new MarketData(dicSettings["INDEX_CAC"]));
                otherIndices.Add(icedow);
                otherIndices.Add(gbpusd);
                otherIndices.Add(eurusd);
                otherIndices.Add(silver);*/
                var models = new List<Model>();
                //var macD_10_30_90_dax = new ModelMacD(dax, 10, 30, 90);
                var robinhood_eurusd = new ModelRobinHood(eurusd);
                var robinhood_gbpusd = new ModelRobinHood(gbpusd);
                //var robinhood_btcusd = new ModelRobinHood(btcusd);
                var robinhood_silver = new ModelRobinHood(silver);
                var robinhood_dax = new ModelRobinHood(dax);
                //var robinhood_cac = new ModelRobinHood(cac);
                var robinhood_ftse = new ModelRobinHood(ftse);
                var robinhood_dow = new ModelRobinHood(dow, 60, 48, 0, 20, 15, new IndicatorVolume(icedow, 60));
                //models.Add(macD_10_30_90_dax);
                models.Add(robinhood_eurusd);
                models.Add(robinhood_gbpusd);
                //models.Add(robinhood_btcusd);
                //models.Add(robinhood_silver);
                models.Add(robinhood_dax);
                //models.Add(robinhood_cac);
                //models.Add(robinhood_dow);
                //models.Add(robinhood_ftse);
                //models.Add(new ModelANN("WMA_5_2", macD_10_30_90_dax, null, null, otherIndices));
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

