using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLapack;
using NLapack.Matrices;
using NLapack.Numbers;

namespace MidaxLib
{   
    public class Neuron
    {
        protected NeuronalLayer _children;
        public Value Weight = new Value();
        public Value Value = new Value();
        public bool Updated = false;
        public Dictionary<int, double> Deltas = new Dictionary<int,double>();    // one delta per output

        public Neuron(NeuronalLayer children)
        {
            _children = children;
        }

        public virtual double Activation()
        {
            if (Updated)
                return Value.X;
            else
                return Value.X = Math.Tanh((from neuron in _children.Neurons select neuron.Weight.X * neuron.Activation()).Sum() + _children.Weight.X);
        }

        public double Derivative(int idxChild)
        {
            // This function assumes activation has been called beforehand and Value has been updated
            if (idxChild > _children.Neurons.Count)
                throw new ApplicationException("Neuron derivative does not exist");
            if (idxChild <= _children.Neurons.Count)
                return _children.Neurons[idxChild].Value.X * (1.0 - Math.Pow(Value.X,2.0));
            else
                return 1.0 - Math.Pow(Value.X,2.0);
        }

        public void BackPropagate(int idxOutput, double outputDelta)
        {
            Deltas[idxOutput] = Weight.X * outputDelta;
            int idxNeuron = 0;
            foreach (var neuron in _children.Neurons)
                neuron.BackPropagate(idxOutput, Derivative(idxNeuron++) * Deltas[idxOutput]);
        }
    }

    public class NeuronInput : Neuron
    {
        public NeuronInput()
            : base(null)
        {
        }

        public override double Activation()
        {
            return Value.X;
        }
    }

    public class NeuronOutput : Neuron
    {
        public NeuronOutput(NeuronalLayer firstInnerLayer)
            : base(firstInnerLayer)
        {
            Weight.X = 1.0;
        }
    }

    public class NeuronalLayer
    {
        public List<Neuron> Neurons = new List<Neuron>();
        public Value Weight = new Value(1.0);
        
        public NeuronalLayer()
        {
        }

        public void Reset()
        {
            foreach (var neuron in Neurons)
                neuron.Updated = false;
        }

        public List<Value> GetWeights()
        {
            List<Value> weights = (from neuron in Neurons select neuron.Weight).ToList();
            //weights.Add(Weight);
            return weights;
        }

        public List<Value> GetValues()
        {
            return (from neuron in Neurons select neuron.Value).ToList();
        }

        public List<Dictionary<int, double>> GetDeltas()
        {
            return (from neuron in Neurons select neuron.Deltas).ToList();
        }
    }
    
    public class NeuronalNetwork
    {
        NeuronalLayer _inputs;
        NeuronalLayer _outputs;
        List<NeuronalLayer> _innerLayers = new List<NeuronalLayer>();

        public NeuronalNetwork(int nbInputs, int nbOuputs, List<int> innerLayerSizes)
        {
            NeuronalLayer _inputs = new NeuronalLayer();
            NeuronalLayer _outputs = new NeuronalLayer();

            for (int idxInput = 0; idxInput < nbInputs; idxInput++)
                _inputs.Neurons.Add(new NeuronInput());

            NeuronalLayer prevLayer = _inputs;
            for (int idxLayer = 0; idxLayer < innerLayerSizes.Count; idxLayer++)
            {
                var layer = new NeuronalLayer();
                for (int idxNeuron = 0; idxNeuron < innerLayerSizes[idxLayer]; idxNeuron++)
                    layer.Neurons.Add(new Neuron(prevLayer));
                _innerLayers.Add(layer);
                prevLayer = layer;
            }

            for (int idxOutput = 0; idxOutput < nbOuputs; idxOutput++)
                _outputs.Neurons.Add(new NeuronOutput(prevLayer));
        }

        public void CalculateOutput(List<double> inputValues)
        {
            if (_inputs.Neurons.Count != inputValues.Count)
                throw new ApplicationException("Input neuron number does not match the number of input values");

            // set updated flag to false to all neurons in the inner layers
            foreach (var layer in _innerLayers)
                layer.Reset();

            // set the input values
            for (int idxInput = 0; idxInput < _inputs.Neurons.Count; idxInput++)
                _inputs.Neurons[idxInput].Value.X = inputValues[idxInput];

            // compute the output values
            for (int idxOutput = 0; idxOutput < _outputs.Neurons.Count; idxOutput++)
                _outputs.Neurons[idxOutput].Activation();
        }

        public List<double> GetOutput()
        {
            List<double> outputValues = new List<double>();
            for (int idxOutput = 0; idxOutput < _outputs.Neurons.Count; idxOutput++)
                outputValues.Add(_outputs.Neurons[idxOutput].Value.X);
            return outputValues;
        }

        public void BackPropagate(List<double> outputValues)
        {
            for (int idxOutput = 0; idxOutput < _outputs.Neurons.Count; idxOutput++)
                _outputs.Neurons[idxOutput].BackPropagate(idxOutput, 1.0);
        }

        public void Train(List<List<double>> inputValues, List<List<double>> outputValues)
        {
            int nbInputs = inputValues[0].Count;
            NRealMatrix inputTable = new NRealMatrix(inputValues.Count, nbInputs);
            for(int idxInputList = 0; idxInputList < inputValues.Count; idxInputList++){
                for(int idxInput = 0; idxInput < inputValues[idxInputList].Count; idxInput++)
                    inputTable[idxInputList,idxInput] = inputValues[idxInputList][idxInput];
            } 

            int nbOutputs = outputValues[0].Count;
            NRealMatrix objectiveTable = new NRealMatrix(outputValues.Count, nbOutputs);
            for(int idxOutputList = 0; idxOutputList < outputValues.Count; idxOutputList++){
                for(int idxOutput = 0; idxOutput < outputValues[idxOutputList].Count; idxOutput++)
                    objectiveTable[idxOutputList,idxOutput] = outputValues[idxOutputList][idxOutput];
            }    

            LevenbergMarquardt.objective_func objFunc = (NRealMatrix x) => { NRealMatrix y = new NRealMatrix(x.Rows, nbOutputs);
                                                 for (int idxRow = 0; idxRow < y.Rows; idxRow++){
                                                     for (int idxCol = 0; idxCol < nbOutputs; idxCol++)
                                                         y[idxRow, idxCol] = objectiveTable[Convert.ToInt32(x[idxRow,0]), idxCol];
                                                 }
                                                 return y;  };

            List<double> inputs = new List<double>();
            for(int idxOutputList = 0; idxOutputList < outputValues.Count; idxOutputList++)
                inputs.Add((double)idxOutputList);

            List<Value> modelParams = _inputs.GetWeights();
            foreach (var layer in _innerLayers)
                modelParams.AddRange(from weight in layer.GetWeights() select weight);

            List<Value> modelValues = _inputs.GetValues();
            foreach (var layer in _innerLayers)
                modelValues.AddRange(from val in layer.GetValues() select val);

            List<Dictionary<int, double>> modelDeltas = _inputs.GetDeltas();
            foreach (var layer in _innerLayers)
                modelDeltas.AddRange(from deltas in layer.GetDeltas() select deltas);

            LevenbergMarquardt.model_func modelFunc = (NRealMatrix x, NRealMatrix weights) => { NRealMatrix y = new NRealMatrix(x.Rows, nbOutputs);
                                                List<double> modelInputs = new List<double>();
                                                for (int idxRow = 0; idxRow < x.Rows; idxRow++){
                                                    CalculateOutput(inputValues[Convert.ToInt32(x[idxRow,0])]); 
                                                    List<double> modelOutputs = GetOutput();
                                                    BackPropagate(modelOutputs);
                                                    for (int idxCol = 0; idxCol < nbOutputs; idxCol++)
                                                        y[idxRow, idxCol] = modelOutputs[idxCol];
                                                } 
                                                return y; };
            
            LevenbergMarquardt.model_func jacFunc = (NRealMatrix x, NRealMatrix weights) => { NRealMatrix jac = new NRealMatrix(weights.Rows, x.Rows);
                                                for (int idxRow = 0; idxRow < jac.Rows; idxRow++){
                                                    for (int idxCol = 0; idxCol < jac.Columns; idxCol++)
                                                        jac[idxRow, idxCol] = modelDeltas[idxRow][Convert.ToInt32(x[idxCol,0])] * modelValues[idxRow].X;
                                                }
                                                return jac; };

            LevenbergMarquardt optimizer = new LevenbergMarquardt(objFunc, inputs, modelParams, modelFunc, jacFunc);
            optimizer.Solve();
        }
    }
}
