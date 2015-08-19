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

public class Server
{
    public class App : Ice.Application
    {
        Model _model = null;

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
                Model.Settings = dicSettings;

                Ice.ObjectAdapter adapter = communicator().createObjectAdapter("MidaxIce");
                Ice.Properties properties = communicator().getProperties();
                Ice.Identity id = communicator().stringToIdentity(properties.getProperty("Identity"));
                
                IGConnection.Instance.Init(dicSettings["APP_NAME"],dicSettings["API_KEY"],dicSettings["USER_NAME"],dicSettings["PASSWORD"]);

                MarketData index = new MarketData(dicSettings["INDEX"], new TimeSeries());
                List<MarketData> stocks = new List<MarketData>();
                foreach (string stock in stockList)
                    stocks.Add(new MarketData(stock, new TimeSeries()));
                _model = new ModelMidax(index, stocks);
                adapter.add(new MidaxIceI(_model, properties.getProperty("Ice.ProgramName")), id);                
                adapter.activate();

                Log.Instance.WriteEntry("Midax service initialized", EventLogEntryType.Information);
                _model.StartSignals();
                communicator().waitForShutdown();
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry(exc.ToString(), EventLogEntryType.Error);
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

