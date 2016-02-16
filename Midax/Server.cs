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
                               
                var index = new MarketData(dicSettings["INDEX"]);
                List<MarketData> stocks = new List<MarketData>();
                foreach (string stock in stockList)
                    stocks.Add(new MarketData(stock));
                List<MarketData> otherIndices = new List<MarketData>();
                otherIndices.Add(new MarketData(dicSettings["INDEX_CAC"]));
                otherIndices.Add(new MarketData(dicSettings["INDEX_DOW"]));
                var models = new List<Model>();
                var macD_10_30_90 = new ModelMacD(index, 10, 30, 90);
                models.Add(macD_10_30_90);
                models.Add(new ModelANN(macD_10_30_90, stocks, new MarketData(dicSettings["VOLATILITY"]), otherIndices));
                models.Add(new ModelMacDCascade(macD_10_30_90));
                //models.Add(new ModelMole(macD_10_30_90));
                _trader = new Trader(models, communicator().shutdown);
                adapter.add(new MidaxIceI(_trader, properties.getProperty("Ice.ProgramName")), id);                
                adapter.activate();

                _trader.Init(Config.GetNow);

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

