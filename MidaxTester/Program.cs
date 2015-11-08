using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MidaxLib;
using NLapack.Matrices;
using NLapack.Numbers;

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
            dicSettings["REPLAY_MODE"] = "CSV";
            dicSettings["REPLAY_CSV"] = TestList(tests);
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_START_TIME"] = "2015-08-26 08:00:00";
            dicSettings["TRADING_STOP_TIME"] = "2015-08-26 09:00:00";
            dicSettings["TRADING_MODE"] = "REPLAY";
            dicSettings["MINIMUM_BET"] = "2";
            Config.Settings = dicSettings;

            Console.WriteLine("Testing calibration...");

            LevenbergMarquardt.objective_func objFunc = (NRealMatrix x) => { NRealMatrix y = new NRealMatrix(x.Rows, 1);
                                                 for (int idxRow = 0; idxRow < y.Rows; idxRow++)
                                                     y.SetAt(idxRow, 0, new NDouble(2 * Math.Cos(x[idxRow, 0]) + Math.Sin(2 * x[idxRow, 0])));
                                                 return y; };
            List<double> inputs = new List<double>();
            Random rnd = new Random(155);    
            for (int idxPt = 0; idxPt < 20; idxPt++)
                inputs.Add(rnd.NextDouble() * 2);
            List<double> modelParams = new List<double>(); 
            modelParams.Add(1.8); modelParams.Add(1.2);
            LevenbergMarquardt.model_func modelFunc = (NRealMatrix x, NRealMatrix weights) => { NRealMatrix y = new NRealMatrix(x.Rows, 1);
                                                double a = weights[0, 0]; double b = weights[1, 0];                                                
                                                for (int idxRow = 0; idxRow < y.Rows; idxRow++)
                                                     y.SetAt(idxRow, 0, new NDouble(a * Math.Cos(b * x[idxRow, 0]) + b * Math.Sin(a * x[idxRow, 0])));
                                                return y; };
            Func<double,double,double,double> derA = (double a, double b, double x) => Math.Cos(b * x) + b * x * Math.Cos(a * x);
            Func<double,double,double,double> derB = (double a, double b, double x) => - a * x * Math.Sin(b * x) + Math.Sin(a * x);
            LevenbergMarquardt.model_func jacFunc = (NRealMatrix x, NRealMatrix weights) => { NRealMatrix jac = new NRealMatrix(2, x.Rows);
                                                double a = weights[0, 0]; double b = weights[1, 0];                                                
                                                for (int idxCol = 0; idxCol < jac.Columns; idxCol++){
                                                    jac.SetAt(0, idxCol, new NDouble(derA(a, b, x[idxCol, 0])));
                                                    jac.SetAt(1, idxCol, new NDouble(derB(a, b, x[idxCol, 0])));
                                                }
                                                return jac; };
            LevenbergMarquardt calibModel = new LevenbergMarquardt(objFunc, inputs, modelParams, modelFunc, jacFunc, 0.001, 0.001, 1000);
            calibModel.Solve();
            if (Math.Abs(modelParams[0] - 2) > calibModel.ObjectiveError || Math.Abs(modelParams[1] - 1) > calibModel.ObjectiveError)
                throw new ApplicationException("LevenbergMarquardt calibration error");

            MarketDataConnection.Instance.Connect(null);
            
            MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP", new TimeSeries());

            List<MarketData> marketData = new List<MarketData>();
            marketData.Add(new MarketData("Adidas AG:ED.D.ADSGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Allianz SE:ED.D.ALVGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("BASF SE:ED.D.BAS.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Bayer AG:ED.D.BAY.DAILY.IP", new TimeSeries()));

            ModelTest model = new ModelTest(index, marketData);
            Console.WriteLine("Testing live indicators and signals...");
            model.StartSignals();
            Console.WriteLine("Testing daily indicators...");
            string status = model.StopSignals();

            if (!dicSettings.ContainsKey("PUBLISHING_CSV"))
            {
                // Test exceptions
                Console.WriteLine("Testing expected exceptions...");
                string expected;
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
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1_IX.D.DAX.DAILY.IP time 08:41 expected value 9975.133333333333333333333413 != 9975.323333333333333333333414";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                try
                {
                    model.StopSignals();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360168776371308016877542 != 9972.779391891891891891891958";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");

                if (status != "Tests passed successfully")
                    model.ProcessError(status);
            }
            Console.WriteLine(status);

            if (dicSettings["REPLAY_POPUP"] == "1")
                MessageBox.Show(status);
        }

        static string TestList(List<string> tests)
        {
            return tests.Aggregate("", (prev, next) => prev + next + ";", res => res.Substring(0, res.Length - 1));
        }
    }
}
