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
        public static void Run()
        {
            List<string> tests = new List<string>();
            tests.Add(@"..\..\expected_results\mktdata_26_8_2015.csv");

            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["PUBLISHING_START_TIME"] = "2015-08-26 00:00:01";
            dicSettings["PUBLISHING_STOP_TIME"] = "2015-08-26 23:59:59";
            //dicSettings["DB_CONTACTPOINT"] = "192.168.1.26";
            //dicSettings["PUBLISHING_CSV"] = @"..\..\expected_results\new_results.csv";   // uncomment this line to generate new test results
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
            
            Console.WriteLine("Testing calibration...");

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

            var index = new Asset("DAX:IX.D.DAX.DAILY.IP", Config.ParseDateTimeLocal(dicSettings["TRADING_START_TIME"]));
            var model = new ModelQuickTest(index);
            ReplayStreamingClient.PTF = model.PTF;
            Console.WriteLine("Testing live indicators and signals...");
            model.StartSignals();
            
            Console.WriteLine("Testing daily indicators...");            
            model.StopSignals();
            model.PublishMarketLevels();
                
            if (!dicSettings.ContainsKey("PUBLISHING_CSV"))
            {
                // test that the right numer of trades was placed. this is an extra sanity check to make sure the program is not idle
                if (ReplayTester.Instance.NbProducedTrades != ReplayTester.Instance.NbExpectedTrades)
                    model.ProcessError(string.Format("the model did not produced the expected number of trades. It produced {0} trades instead of {1} expected",
                                                    ReplayTester.Instance.NbProducedTrades, ReplayTester.Instance.NbExpectedTrades));
                // Test exceptions. the program is expected to throw exceptions here, just press continue if you are debugging
                // all exceptions should be handled, and the program should terminate with a success message box
                Console.WriteLine("Testing expected exceptions...");
                string expected;
                List<string> testError = new List<string>();
                testError.Add(@"..\..\expected_results\mktdata_26_8_2015_error.csv");                
                MarketDataConnection.Instance.Connect(null);                
                bool success = false;
                ModelMacDTest modelBis = new ModelQuickTest(index);
                ReplayStreamingClient.PTF = modelBis.PTF;
                dicSettings["REPLAY_CSV"] = Config.TestList(testError);
                var modelErr = new ModelQuickTest(index);
                try
                {
                    modelBis.StartSignals();
                    MarketDataConnection.Instance.Connect(null);
                    ReplayStreamingClient.PTF = modelErr.PTF;
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
                try
                {
                    modelErr.StopSignals();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360169 != 9970.69755260538438389765106";
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
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360169 != 9970.69755260538438389765106";
                    success = (exc.Message == expected);
                    if (!success)
                        model.ProcessError(exc.Message, expected);
                }
                success = false;
                try
                {
                    MarketDataConnection.Instance = new ReplayConnection();
                    MarketDataConnection.Instance.Connect(null);
                    model = new ModelQuickTest(new Asset(index.Id, Config.ParseDateTimeLocal(dicSettings["TRADING_START_TIME"])));
                    ReplayStreamingClient.PTF = model.PTF;
                    model.StartSignals();
                }
                catch (Exception exc)
                {
                    expected = "Test failed: indicator WMA_1_IX.D.DAX.DAILY.IP time 08:41 expected value 9976.135 != 9975.736666666666666666666747";
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
                    expected = "Test failed: indicator WMA_1D_IX.D.DAX.DAILY.IP time 23:59 expected value 9964.360169 != 9983.779772679923146369004401";
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
