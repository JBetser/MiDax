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
    public class Core
    {
        public static void Run(bool generate = false, bool generate_from_db = false)
        {           
            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["PUBLISHING_START_TIME"] = "2016-01-22 08:30:00";
            dicSettings["PUBLISHING_STOP_TIME"] = "2016-01-22 09:00:00";
            dicSettings["REPLAY_MODE"] = "CSV";  
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_START_TIME"] = "2016-01-22 08:45:00";
            dicSettings["TRADING_STOP_TIME"] = "2016-01-22 08:59:00";
            dicSettings["TRADING_CLOSING_TIME"] = "2016-01-22 08:57:00";
            dicSettings["TRADING_MODE"] = "REPLAY";
            dicSettings["TRADING_SIGNAL"] = "MacD_1_5_IX.D.DAX.DAILY.IP";
            dicSettings["TRADING_LIMIT_PER_BP"] = "10";
            dicSettings["TRADING_CURRENCY"] = "GBP";
            Config.Settings = dicSettings;

            string action = generate ? "Generating" : "Testing";
            var index = new MarketData("DAX:IX.D.DAX.DAILY.IP");            
            List<string> tests = new List<string>();

            Console.WriteLine(action + " WMA...");
            // Test weighted moving average with long intervals
            tests.Add(@"..\..\expected_results\testWMA.csv");
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\testWMAgen.csv");
            var macDTestWMA = new ModelMacDTest(index,1,2,3);
            MarketDataConnection.Instance.Connect(null);
            macDTestWMA.StartSignals();
            macDTestWMA.StopSignals();

            // Test weighted moving average with short intervals
            tests = new List<string>();
            tests.Add(@"..\..\expected_results\testWMA2.csv");
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\testWMA2gen.csv");
            macDTestWMA = new ModelMacDTest(index, 1, 2, 3);
            MarketDataConnection.Instance.Connect(null);
            macDTestWMA.StartSignals();
            macDTestWMA.StopSignals();

            // Test weighted moving average with linear time decay
            tests = new List<string>();
            tests.Add(@"..\..\expected_results\testWMA3.csv");
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            dicSettings["TIME_DECAY_FACTOR"] = "3";
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\testWMA3gen.csv");
            macDTestWMA = new ModelMacDTest(index, 1, 2, 3);
            MarketDataConnection.Instance.Connect(null);
            macDTestWMA.StartSignals();
            macDTestWMA.StopSignals();

            // Test volume weighted moving average with linear time decay
            tests = new List<string>();
            tests.Add(@"..\..\expected_results\testWMA4.csv");
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\testWMA4gen.csv");
            var macDVTest = new ModelMacDVTest(index, 1, 2, 3);
            MarketDataConnection.Instance.Connect(null);
            macDVTest.StartSignals();
            macDVTest.StopSignals();
            dicSettings.Remove("TIME_DECAY_FACTOR");

            // Test RSI and Correlation indicators
            tests = new List<string>();
            tests.Add(@"..\..\expected_results\testRsiCorrel.csv");
            dicSettings["INDEX_ICEDOW"] = "DOW:IceConnection_DOW";
            dicSettings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            dicSettings["INDEX_DAX"] = "DAX:IX.D.DAX.DAILY.IP";
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\testRsiCorrelgen.csv");
            var dax = new MarketData(dicSettings["INDEX_DAX"]);
            var iceindex = new MarketData(dicSettings["INDEX_ICEDOW"]);
            index = new MarketData(dicSettings["INDEX_DOW"]);
            var macD = new ModelMacDTest(dax, 1, 2, 3);
            var macDV = new ModelMacDVTest(iceindex, 1, 2, 3, index);
            var moleTest = new ModelMoleTest(macDV);
            MarketDataConnection.Instance.Connect(null);
            macD.StartSignals(false);
            macDV.StartSignals(false);
            moleTest.StartSignals(false);
            MarketDataConnection.Instance.StartListening();
            moleTest.StopSignals(false);
            macDV.StartSignals(false);
            macD.StopSignals(false);
            MarketDataConnection.Instance.StopListening();

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

            Console.WriteLine(action + " live indicators and signals...");
            tests = new List<string>();
            tests.Add(@"..\..\expected_results\core_22_1_2016.csv");
            if (generate_from_db)
                dicSettings["DB_CONTACTPOINT"] = "192.168.1.26";            
            dicSettings["REPLAY_MODE"] = generate_from_db ? "DB" : "CSV";  
            dicSettings["REPLAY_CSV"] = Config.TestList(tests);
            if (generate)
                dicSettings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\coregen_22_1_2016.csv");
            MarketDataConnection.Instance.Connect(null);
            var model = new ModelMacDTest(index); 
            model.StartSignals();
            
            Console.WriteLine(action + " daily indicators...");            
            model.StopSignals();
            Thread.Sleep(1000); 

            if (!dicSettings.ContainsKey("PUBLISHING_CSV"))
            {
                // the program is expected to throw exceptions in this scope, just press continue if you are debugging
                // all exceptions should be handled, and the program should terminate with a success message box

                // test that the right numer of trades was placed. this is an extra sanity check to make sure the program is not idle
                if (ReplayTester.Instance.NbProducedTrades != ReplayTester.Instance.NbExpectedTrades)
                    model.ProcessError(string.Format("the model did not produced the expected number of trades. It produced {0} trades instead of {1} expected",
                                                    ReplayTester.Instance.NbProducedTrades, ReplayTester.Instance.NbExpectedTrades));

                // test trade booking
                MarketDataConnection.Instance = new ReplayConnection();
                model = new ModelMacDTest(index);
                MarketDataConnection.Instance.Connect(null);
                Console.WriteLine(action + " trade booking...");
                var tradeTime = Config.ParseDateTimeLocal(dicSettings["TRADING_CLOSING_TIME"]).AddSeconds(-1);
                var tradeTest = new Trade(tradeTime, index.Id, SIGNAL_CODE.SELL, 10, 10000m);
                var expectedTrades = new Dictionary<KeyValuePair<string, DateTime>, Trade>();
                expectedTrades[new KeyValuePair<string, DateTime>("###DUMMY_TRADE_REF1###", tradeTime)] = tradeTest;
                ReplayTester.Instance.SetExpectedResults(null, null, expectedTrades, null);
                model.PTF.Subscribe();
                model.PTF.BookTrade(tradeTest);
                Thread.Sleep(1000);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != -10)
                    throw new ApplicationException("SELL Trade booking error");
                var expectedTrade = new Trade(tradeTime, index.Id, SIGNAL_CODE.BUY, 10, 10000m);
                expectedTrade.Reference = "###CLOSE_DUMMY_TRADE_REF2###";
                expectedTrade.Id = "###DUMMY_TRADE_ID1###";
                expectedTrades[new KeyValuePair<string, DateTime>(expectedTrade.Reference, tradeTime)] = expectedTrade;
                model.PTF.ClosePosition(tradeTest, tradeTime);
                Thread.Sleep(1000);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != 0)
                    throw new ApplicationException("Trade position closing error");
                expectedTrade.Reference = "###DUMMY_TRADE_REF3###";
                expectedTrade.Id = "###DUMMY_TRADE_ID2###";
                expectedTrades[new KeyValuePair<string, DateTime>(expectedTrade.Reference, tradeTime)] = expectedTrade;
                model.PTF.BookTrade(new Trade(tradeTest, true, tradeTime));
                Thread.Sleep(1000);
                if (model.PTF.GetPosition(tradeTest.Epic).Quantity != 10)
                    throw new ApplicationException("BUY Trade booking error");
                expectedTrade = new Trade(tradeTime, index.Id, SIGNAL_CODE.SELL, 10, 0m);
                expectedTrade.Reference = "###CLOSE_DUMMY_TRADE_REF4###";
                expectedTrade.Id = "###DUMMY_TRADE_ID2###";
                expectedTrades[new KeyValuePair<string, DateTime>(expectedTrade.Reference, tradeTime)] = expectedTrade;
                Portfolio.Instance.CloseAllPositions(tradeTest.TradingTime);
                Thread.Sleep(1000);

                // test synchronization issues with the broker
                List<string> testsSync = new List<string>();
                testsSync.Add(@"..\..\expected_results\sync.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testsSync);
                MarketDataConnection.Instance = new ReplayCrazySeller();
                model = new ModelMacDTest(index);
                Console.WriteLine(action + " synchronization...");
                MarketDataConnection.Instance.Connect(null);
                model.StartSignals();
                model.StopSignals();
                testsSync = new List<string>();
                testsSync.Add(@"..\..\expected_results\sync2.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testsSync);
                MarketDataConnection.Instance = new ReplayCrazyBuyer();
                model = new ModelMacDTest(index);
                MarketDataConnection.Instance.Connect(null);
                model.StartSignals();
                model.StopSignals();
                                
                Console.WriteLine(action + " expected exceptions...");
                dicSettings["REPLAY_CSV"] = Config.TestList(tests);
                MarketDataConnection.Instance = new ReplayConnection();
                MarketDataConnection.Instance.Connect(null);
                List<string> testError = new List<string>();
                testError.Add(@"..\..\expected_results\error.csv");
                dicSettings["REPLAY_CSV"] = Config.TestList(testError);
                var modelErr = new ModelMacDTest(index);
                string expected;
                bool success = false;
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
                    model.ProcessError(exc.Message + " - Wrong daily mean exception removed");
                }
                success = false;
                try
                {
                    model.StopSignals();
                }
                catch (Exception exc)
                {
                    model.ProcessError(exc.Message + " - Double EOD publishing exception removed");
                }
                success = false;
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
                    MarketDataConnection.Instance.Resume();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1_IX.D.DAX.DAILY.IP time 08:31 expected value 9735.710930440771349862258972 != 9735.705972222222222222222239";
                    success = (exc.Message.Replace(" AM", "") == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                if (!success)
                    model.ProcessError("An expected exception has not been thrown");
                model.StopSignals();
                success = false;
            }            
        }
    }
}
