using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MidaxLib;
using NLapack.Matrices;
using NLapack.Numbers;

namespace MidaxTester
{
    public class Core
    {
        public static void Run(bool generate = false)
        {
            List<string> tests = new List<string>();
            tests.Add(@"..\..\expected_results\mktdata_26_8_2015.csv");

            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["PUBLISHING_START_TIME"] = "2015-08-26 00:00:01";
            dicSettings["PUBLISHING_STOP_TIME"] = "2015-08-26 23:59:59";
            //dicSettings["DB_CONTACTPOINT"] = "192.168.1.26";      // uncomment this line to replay from the DB instead of the csv files
            if (generate)
                dicSettings["PUBLISHING_CSV"] = @"..\..\expected_results\mktdatagen_26_8_2015.csv"; 
            dicSettings["REPLAY_MODE"] = "CSV";
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_START_TIME"] = "2015-08-26 08:00:00";
            dicSettings["TRADING_STOP_TIME"] = "2015-08-26 09:00:00";
            dicSettings["TRADING_CLOSING_TIME"] = "2015-08-26 08:57:00";
            dicSettings["TRADING_MODE"] = "REPLAY";
            dicSettings["TRADING_SIGNAL"] = "MacD_1_5_IX.D.DAX.DAILY.IP";
            dicSettings["TRADING_LIMIT_PER_BP"] = "10";
            dicSettings["TRADING_CURRENCY"] = "GBP";
            Config.Settings = dicSettings;

            string action = generate ? "Generating" : "Testing";
            Console.WriteLine(action + " calibration...");

            // Test a 1mn linear regression
            var mktData = new MarketData("testLRMktData");
            var updateTime = Config.ParseDateTimeLocal(dicSettings["TRADING_START_TIME"]);
            mktData.TimeSeries.Add(updateTime, new Price(100));
            mktData.TimeSeries.Add(updateTime.AddSeconds(20), new Price(120));
            mktData.TimeSeries.Add(updateTime.AddSeconds(40), new Price(140));
            mktData.TimeSeries.Add(updateTime.AddSeconds(60), new Price(130));
            mktData.TimeSeries.Add(updateTime.AddSeconds(80), new Price(145));
            mktData.TimeSeries.Add(updateTime.AddSeconds(100), new Price(165));
            mktData.TimeSeries.Add(updateTime.AddSeconds(120), new Price(145));
            var linReg = new IndicatorLinearRegression(mktData, new TimeSpan(0, 2, 0));
            var linRegCoeff = linReg.linearCoeff(updateTime.AddSeconds(120));
            if (Math.Abs(linRegCoeff.Value - 0.821428571428573m) > 1e-8m)
                throw new ApplicationException("Linear regression error");
            

            // Test the optimization of function a * cos(b * x) + b * sin(a * x) using Levenberg Marquardt
            LevenbergMarquardt.objective_func objFunc = (NRealMatrix x) => { NRealMatrix y = new NRealMatrix(x.Rows, 1);
                                                 for (int idxRow = 0; idxRow < y.Rows; idxRow++)
                                                     y.SetAt(idxRow, 0, new NDouble(2 * Math.Cos(x[idxRow, 0]) + Math.Sin(2 * x[idxRow, 0])));
                                                 return y; };
            List<double> inputs = new List<double>();
            Random rnd = new Random(155);    
            for (int idxPt = 0; idxPt < 10; idxPt++)
                inputs.Add(rnd.NextDouble() * 2);
            List<Value> modelParams = new List<Value>();
            modelParams.Add(new Value(-0.2)); modelParams.Add(new Value(0.3));
            LevenbergMarquardt.model_func modelFunc = (NRealMatrix x, NRealMatrix weights) => { NRealMatrix y = new NRealMatrix(x.Rows, 1);
                                                double a = weights[0, 0]; double b = weights[0, 1];                                                
                                                for (int idxRow = 0; idxRow < y.Rows; idxRow++)
                                                     y.SetAt(idxRow, 0, new NDouble(a * Math.Cos(b * x[idxRow, 0]) + b * Math.Sin(a * x[idxRow, 0])));
                                                return y; };
            Func<double,double,double,double> derA = (double a, double b, double x) => Math.Cos(b * x) + b * x * Math.Cos(a * x);
            Func<double,double,double,double> derB = (double a, double b, double x) => - a * x * Math.Sin(b * x) + Math.Sin(a * x);
            LevenbergMarquardt.model_func jacFunc = (NRealMatrix x, NRealMatrix weights) =>
            {
                NRealMatrix jac = new NRealMatrix(x.Rows, 2);
                double a = weights[0, 0]; double b = weights[0, 1];
                for (int idxRow = 0; idxRow < jac.Rows; idxRow++)
                {
                    jac.SetAt(idxRow, 0, new NDouble(-derA(a, b, x[idxRow, 0])));
                    jac.SetAt(idxRow, 1, new NDouble(-derB(a, b, x[idxRow, 0])));
                }
                return jac; 
            };
            LevenbergMarquardt calibModel = new LevenbergMarquardt(objFunc, inputs, modelParams, modelFunc, jacFunc);
            calibModel.Solve();
            if (Math.Abs(modelParams[0].X - 2) > calibModel.ObjectiveError || Math.Abs(modelParams[1].X - 1) > calibModel.ObjectiveError)
                throw new ApplicationException("LevenbergMarquardt calibration error");

            // Parity-2 problem
            NeuralNetwork ann = new NeuralNetwork(2, 1, new List<int>() { 2 });
            List<List<double>> annInputs = new List<List<double>>();
            annInputs.Add(new List<double>() { -1, -1 });
            annInputs.Add(new List<double>() { -1, 1 });
            annInputs.Add(new List<double>() { 1, -1 });
            annInputs.Add(new List<double>() { 1, 1 });
            List<List<double>> annOutputs = new List<List<double>>();
            annOutputs.Add(new List<double>() { 1 });
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() { 1 });
            // test forward propagation
            ann._outputs.Neurons[0].Weights[0].X = 1;
            ann._outputs.Neurons[0].Weights[1].X = -1;
            ann._outputs.Neurons[0].Weights[2].X = -1;
            ann._innerLayers[0].Neurons[0].Weights[0].X = 1;
            ann._innerLayers[0].Neurons[0].Weights[1].X = 1;
            ann._innerLayers[0].Neurons[0].Weights[2].X = 1;
            ann._innerLayers[0].Neurons[1].Weights[0].X = 1;
            ann._innerLayers[0].Neurons[1].Weights[1].X = 1;
            ann._innerLayers[0].Neurons[1].Weights[2].X = -1;
            ann._inputs.Neurons[0].Value.X = -1;
            ann._inputs.Neurons[1].Value.X = -1;
            if (Math.Abs(ann._outputs.Neurons[0].Activation() - -0.38873457229297215) > calibModel.ObjectiveError)
                throw new ApplicationException("Neural network forward propagation error");
            // Test neural network training for parity-2 problem
            ann = new NeuralNetwork(2, 1, new List<int>() { 2 });
            ann.Train(annInputs, annOutputs);

            // Test neural network training for parity-3 problem
            ann = new NeuralNetwork(3, 1, new List<int>() { 2 });
            annInputs = new List<List<double>>();
            annInputs.Add(new List<double>() {-1,-1,-1});
            annInputs.Add(new List<double>() {-1,-1, 1});
            annInputs.Add(new List<double>() {-1, 1,-1});
            annInputs.Add(new List<double>() {-1, 1, 1});
            annInputs.Add(new List<double>() { 1,-1,-1});
            annInputs.Add(new List<double>() { 1,-1, 1});
            annInputs.Add(new List<double>() { 1, 1,-1});
            annInputs.Add(new List<double>() { 1, 1, 1});
            annOutputs = new List<List<double>>();
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() {  1 });
            annOutputs.Add(new List<double>() {  1 });
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() {  1 });
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() { -1 });
            annOutputs.Add(new List<double>() {  1 });
            ann.Train(annInputs, annOutputs);

            MarketDataConnection.Instance.Connect(null);

            var index = new MarketData("DAX:IX.D.DAX.DAILY.IP");
            var model = new ModelMacDTest(index);
            
            Console.WriteLine(action + " live indicators and signals...");
            model.StartSignals();
            
            Console.WriteLine(action + " daily indicators...");            
            model.StopSignals();
                
            if (!dicSettings.ContainsKey("PUBLISHING_CSV"))
            {
                // test that the right numer of trades was placed. this is an extra sanity check to make sure the program is not idle
                if (ReplayTester.Instance.NbProducedTrades != ReplayTester.Instance.NbExpectedTrades)
                    model.ProcessError(string.Format("the model did not produced the expected number of trades. It produced {0} trades instead of {1} expected",
                                                    ReplayTester.Instance.NbProducedTrades, ReplayTester.Instance.NbExpectedTrades));

                // test trade booking
                MarketDataConnection.Instance = new ReplayConnection();
                model = new ModelMacDTest(index);
                MarketDataConnection.Instance.Connect(null);
                Console.WriteLine(action + " trade booking...");
                var tradeTime = Config.ParseDateTimeLocal(dicSettings["TRADING_CLOSING_TIME"]);
                var tradeTest = new Trade(tradeTime, index.Id, SIGNAL_CODE.SELL, 10, 10000m);
                var expectedTrades = new Dictionary<KeyValuePair<string, DateTime>, Trade>();
                expectedTrades[new KeyValuePair<string, DateTime>(index.Id, tradeTime)] = tradeTest;
                ReplayTester.Instance.SetExpectedResults(null, null, expectedTrades, null, null); 
                model.BookTrade(tradeTest);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != -10)
                    throw new ApplicationException("SELL Trade booking error");
                var expectedTrade = new Trade(tradeTime, index.Id, SIGNAL_CODE.BUY, 10, 10000m);
                expectedTrade.Reference = "###DUMMY_TRADE_REF###";
                expectedTrades[new KeyValuePair<string, DateTime>(index.Id, tradeTime)] = expectedTrade;
                tradeTest = new Trade(tradeTest, true, tradeTime);
                model.BookTrade(tradeTest);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != 0)
                    throw new ApplicationException("Trade position closing error");
                model.BookTrade(tradeTest);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != 10)
                    throw new ApplicationException("BUY Trade booking error");
                string expected;
                bool success = false;
                expectedTrade = new Trade(tradeTime, index.Id, SIGNAL_CODE.SELL, 10, 10000m);
                expectedTrade.Reference = "###CLOSE_DUMMY_TRADE_REF###";
                expectedTrades[new KeyValuePair<string, DateTime>(index.Id, tradeTime)] = expectedTrade;
                try
                {
                    model.CloseAllPositions(tradeTest.TradingTime);
                }
                catch (Exception exc)
                {
                    expected = "Test failed: trade IX.D.DAX.DAILY.IP expected Price 10000 != 0";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }

                // test synchronization issues with the broker
                List<string> testsSync = new List<string>();
                testsSync.Add(@"..\..\expected_results\mktdata_26_8_2015_sync.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testsSync);
                MarketDataConnection.Instance = new ReplayCrazySeller();
                model = new ModelMacDTest(index);
                Console.WriteLine(action + " synchronization...");
                MarketDataConnection.Instance.Connect(null);
                model.StartSignals();
                model.StopSignals();
                testsSync = new List<string>();
                testsSync.Add(@"..\..\expected_results\mktdata_26_8_2015_sync2.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testsSync);
                MarketDataConnection.Instance = new ReplayCrazyBuyer();
                model = new ModelMacDTest(index);
                MarketDataConnection.Instance.Connect(null);
                model.StartSignals();
                model.StopSignals();

                // Test exceptions. the program is expected to throw exceptions here, just press continue if you are debugging
                // all exceptions should be handled, and the program should terminate with a success message box
                Console.WriteLine(action + " expected exceptions...");
                dicSettings["REPLAY_CSV"] = Config.TestList(tests);
                MarketDataConnection.Instance = new ReplayConnection();
                MarketDataConnection.Instance.Connect(null);
                List<string> testError = new List<string>();
                testError.Add(@"..\..\expected_results\mktdata_26_8_2015_error.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testError);
                var modelErr = new ModelMacDTest(index);
                try
                {
                    MarketDataConnection.Instance.Connect(null);
                    modelErr.StartSignals();
                }
                catch (Exception exc)
                {
                    expected = "Time series do not accept values in the past";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");
                success = false;
                try
                {
                    modelErr.StopSignals();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360169 != 9967.999999999999999999875687";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");
                success = false;
                try
                {
                    model.StopSignals();
                }
                catch (Exception exc)
                {
                    model.ProcessError(exc.Message + " - Double EOD publishing exception removed");
                }
                try
                {
                    MarketDataConnection.Instance = new ReplayConnection();
                    MarketDataConnection.Instance.Connect(null);
                    model = new ModelMacDTest(new MarketData(index.Id));
                    model.StartSignals();
                }
                catch (Exception exc)
                {
                    expected = "Time series do not accept values in the past";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");
                success = false;
                try
                {
                    model.StopSignals();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360169 != 9967.999999999999999999875687";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");
                success = false;
            }            
        }
    }
}
