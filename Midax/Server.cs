using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPModel;
using Midax;

public class Server
{
    public class App : Ice.Application
    {
        Model _model = null;
        public string _sServerManagerPort;

        public override int run(string[] args)
        {           
            try
            {
                if (args.Length != 0)
                    throw new ApplicationException("starting: too many arguments in application call.");

                _sServerManagerPort = Midax.Properties.Settings.Default.PORT_INPUT;

                Dictionary<string, string> dicSettings = new Dictionary<string, string>();
                List<string> stockList = new List<string>();
                foreach (SettingsPropertyValue prop in Midax.Properties.Settings.Default.PropertyValues)
                {
                    if (prop.Name == "STOCKS")
                        stockList = ((string[])prop.PropertyValue).ToList();
                    else
                        dicSettings.Add(prop.Name, (string)prop.PropertyValue);
                }

                Ice.ObjectAdapter adapter = communicator().createObjectAdapter("MidaxIce");
                Ice.Properties properties = communicator().getProperties();
                Ice.Identity id = communicator().stringToIdentity(properties.getProperty("Identity"));

                IGConnection.Instance.Init(dicSettings["APP_NAME"],dicSettings["API_KEY"],dicSettings["USER_NAME"],dicSettings["PASSWORD"]);

                MarketData index = new MarketData(dicSettings["INDEX"], new Dictionary<DateTime, IGPublicPcl.L1LsPriceData>());
                List<MarketData> stocks = new List<MarketData>();
                foreach (string stock in stockList)
                    stocks.Add(new MarketData(stock, new Dictionary<DateTime, IGPublicPcl.L1LsPriceData>()));
                _model = new ModelMidax(index,stocks);
                adapter.add(new MidaxIceI(_model, properties.getProperty("Ice.ProgramName")), id);
                adapter.activate();

                IGConnection.Instance.Log.WriteEntry("Midax service initialized", EventLogEntryType.Information);
                communicator().waitForShutdown();
            }
            catch (Exception exc)
            {
                IGConnection.Instance.Log.WriteEntry(exc.ToString(), EventLogEntryType.Error);
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

