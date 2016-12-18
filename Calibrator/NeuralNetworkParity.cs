using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class NeuralNetworkParity : NeuralNetworkForCalibration
    {
        public NeuralNetworkParity(string id)
            : base(id, "", 4, 1, new List<int>() { 3 })
        {
            _inputs.InputNeurons[0].Label = "n1";
            _inputs.InputNeurons[1].Label = "n2";
            _inputs.InputNeurons[2].Label = "n3";
            _inputs.InputNeurons[3].Label = "n4";
            _outputs.InputNeurons[0].Label = "parity";
            // Test neural network training for parity-3 problem
            _annInputs.Add(new List<double>() { 1, -1, -1, -1 });
            _annInputs.Add(new List<double>() { 1, -1, -1, 1 });
            _annInputs.Add(new List<double>() { 1, -1, 1, -1 });
            _annInputs.Add(new List<double>() { 1, -1, 1, 1 });
            _annInputs.Add(new List<double>() { 1, 1, -1, -1 });
            _annInputs.Add(new List<double>() { 1, 1, -1, 1 });
            _annInputs.Add(new List<double>() { 1, 1, 1, -1 });
            _annInputs.Add(new List<double>() { 1, 1, 1, 1 });
            _annInputs.Add(new List<double>() { -1, -1, -1, -1 });
            _annInputs.Add(new List<double>() { -1, -1, -1, 1 });
            _annInputs.Add(new List<double>() { -1, -1, 1, -1 });
            _annInputs.Add(new List<double>() { -1, -1, 1, 1 });
            _annInputs.Add(new List<double>() { -1, 1, -1, -1 });
            _annInputs.Add(new List<double>() { -1, 1, -1, 1 });
            _annInputs.Add(new List<double>() { -1, 1, 1, -1 });
            _annInputs.Add(new List<double>() { -1, 1, 1, 1 });

            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { -1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { 1 });
            _annOutputs.Add(new List<double>() { -1 });
        }
    }
}
