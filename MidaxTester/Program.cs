using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MidaxLib;

namespace MidaxTester
{   
    class Program
    {
        static void Main(string[] args)
        {      
            List<string> tests = new List<string>();
            tests.Add(@"..\..\expected_results\mktdata_26_8_2015.csv");

            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["STOP_LOSS"] = "50";
            dicSettings["STOP_GAIN"] = "200";
            dicSettings["LIMIT"] = "200";
            dicSettings["PUBLISHING_START_TIME"] = "2015-08-26 00:00:01";
            dicSettings["PUBLISHING_STOP_TIME"] = "2015-08-26 23:59:59";
            dicSettings["PUBLISHING_DISABLED"] = "1";
            dicSettings["PUBLISHING_CONTACTPOINT"] = "192.168.1.26";
            //dicSettings["PUBLISHING_CSV"] = @"..\..\expected_results\new_results.csv";   // uncomment this line to generate new test results
            dicSettings["REPLAY_CSV"] = TestList(tests);
            dicSettings["TRADING_START_TIME"] = "2015-08-26 08:00:00";
            dicSettings["TRADING_STOP_TIME"] = "2015-08-26 09:00:00";
            dicSettings["TRADING_MODE"] = "REPLAY";
            dicSettings["REPLAY_MODE"] = "CSV";
            dicSettings["MINIMUM_BET"] = "2";
            Config.Settings = dicSettings;

            MarketDataConnection.Instance.Connect(null);
            
            MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP", new TimeSeries());
            List<MarketData> marketData = new List<MarketData>();
            marketData.Add(new MarketData("Adidas AG:ED.D.ADSGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Allianz SE:ED.D.ALVGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("BASF SE:ED.D.BAS.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Bayer AG:ED.D.BAY.DAILY.IP", new TimeSeries()));

            ModelTest model = new ModelTest(index, marketData);
            model.StartSignals();
            string status = model.StopSignals();

            if (!dicSettings.ContainsKey("PUBLISHING_CSV"))
            {
                // Test exceptions
                List<string> testError = new List<string>();
                testError.Add(@"..\..\expected_results\mktdata_26_8_2015_error.csv");
                dicSettings["REPLAY_CSV"] = TestList(testError);
                MarketDataConnection.Instance.Connect(null);
                model = new ModelTest(index, marketData);
                bool success = false;
                try
                {
                    model.StartSignals();
                }
                catch (ApplicationException exc)
                {
                    success = (exc.Message == "Test failed: indicator WMA_1_IX.D.DAX.DAILY.IP time 08:41 expected value 9975.133333333333333333333413 != 9975.323333333333333333333414");
                    if (!success)
                        throw new ApplicationException("An exception message test failed");
                }
                finally
                {
                    try
                    {
                        model.StopSignals();
                    }
                    catch (Exception exc)
                    {
                        success = (exc.Message == "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360168776371308016877542 != 9972.779391891891891891891958");
                        if (!success)
                            throw new ApplicationException("An exception message test failed");
                    }
                }
                if (!success)
                    throw new ApplicationException("An expected exception has not been thrown");
            }
            MessageBox.Show(status);
        }

        static string TestList(List<string> tests)
        {
            return tests.Aggregate("", (prev, next) => prev + next + ";", res => res.Substring(0, res.Length - 1));
        }
    }
}
