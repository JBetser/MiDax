using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BPModel;

namespace MidaxTester
{   
    class Program
    {
        static void Main(string[] args)
        {
            string appName = "Midax";
            string apiKey = "8d341413c2eae2c35bb5b47a594ef08ae18cb3b7";
            string userName = "ksbitlsoftdemo";
            string password = "Kotik0483";

            IGConnection.Instance.Init(appName, apiKey, userName, password);
            MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP", new Dictionary<DateTime, IGPublicPcl.L1LsPriceData>());
            List<MarketData> marketData = new List<MarketData>();
            marketData.Add(new MarketData("Adidas AG:ED.D.ADSGY.DAILY.IP", new Dictionary<DateTime,IGPublicPcl.L1LsPriceData>()));
            marketData.Add(new MarketData("Allianz SE:ED.D.ALVGY.DAILY.IP", new Dictionary<DateTime,IGPublicPcl.L1LsPriceData>()));
            marketData.Add(new MarketData("BASF SE:ED.D.BAS.DAILY.IP", new Dictionary<DateTime,IGPublicPcl.L1LsPriceData>()));
            marketData.Add(new MarketData("Bayer AG:ED.D.BAY.DAILY.IP", new Dictionary<DateTime,IGPublicPcl.L1LsPriceData>()));
            ModelMidax model = new ModelMidax(index, marketData);
            model.StartSignals();
            while (true)
                Thread.Sleep(1000);
        }
    }
}
