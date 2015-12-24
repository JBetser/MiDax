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
        protected NeuralLayer _children;
        protected NeuralLayer _parents;
        public NeuralLayer Children { get { return _children; } }
        public NeuralLayer Parents { get { return _parents; } }
        public List<Value> Weights = new List<Value>();
        public Value Value = new Value();
        public bool Updated = false;
        public List<Value> Deltas = new List<Value>();
        private const double _activationCoeff = 1.7159;

        public Neuron(NeuralLayer children, NeuralLayer parentLayer)
        {
            _parents = parentLayer;
            _children = children;
            int size = children.Size;
            while (size--> 0)
                Weights.Add(new Value());
            if (children.Size > 0) // Bias neuron
                Weights.Add(new Value());
            size = parentLayer.Size;
            while (size--> 0)
                Deltas.Add(new Value()); 
        }

        public virtual double Activation()
        {
            if (!Updated)
                Value.X = _activationCoeff * Math.Tanh(2.0 * (from neuron in _children.Neurons select Weights[_children.Neurons.IndexOf(neuron)].X * neuron.Activation()).Sum() / 3.0);
            Updated = true;
            return Value.X;
        }

        public double Derivative()
        {
            return (1.0 - Math.Pow(Value.X / _activationCoeff, 2.0)) * _activationCoeff * 2.0 / 3.0;
        }

        public virtual void BackPropagate()
        {
            foreach (var neuron in _children.Neurons)
            {
                int idxDelta = neuron.Parents.Neurons.IndexOf(this);
                double sumDelta = 0.0;
                int idxNeuron = 0;
                foreach (var parent in Parents.Neurons)
                {
                    int idxChild = parent.Children.Neurons.IndexOf(this);
                    sumDelta += Deltas[idxNeuron++].X * parent.Weights[idxChild].X;
                }
                neuron.Deltas[idxDelta].X = Derivative() * sumDelta;
            }
        }

        public void GetInputValues(List<Value> values)
        {
            foreach (var neuron in _children.Neurons)
                values.Add(neuron.Value);
        }

        public void GetInputDeltas(List<Value> deltas)
        {
            foreach (var neuron in _children.Neurons)
                deltas.Add(neuron.Deltas[neuron.Parents.Neurons.IndexOf(this)]);
        }

        public void GetOutputValues(List<Value> values)
        {
            values.Add(Value);
        }        
    }

    public class NeuronInput : Neuron
    {
        public NeuronInput(NeuralLayer firstInnerLayer)
            : base(new NeuralLayer(0), firstInnerLayer)
        {
        }

        public override double Activation()
        {
            return Value.X;
        }
    }

    public class NeuronOutput : Neuron
    {
        public NeuronOutput(NeuralLayer outputLayer)
            : base(outputLayer, new NeuralLayer(0))
        {
        }
        
        public override void BackPropagate()
        {
            foreach (var neuron in _children.Neurons)
            {
                int idxParentDelta = neuron.Parents.Neurons.IndexOf(this);
                neuron.Deltas[idxParentDelta].X = Derivative();
            }
        }
    }

    public class NeuronBias : NeuronInput
    {
        public NeuronBias(NeuralLayer nextLayer)
            : base(nextLayer)
        {
            Value.X = 1.0;
        }
    }

    public class NeuralLayer
    {
        public int Size = 0;
        public List<Neuron> Neurons = new List<Neuron>();
        public List<Neuron> InputNeurons
        {
            get { return (from Neuron neuron in Neurons where neuron.GetType() != typeof(NeuronBias) select neuron).ToList(); } // excludes the bias 
        }
        public NeuronBias Bias = null;
        
        public NeuralLayer(int size)
        {
            Size = size;
        }

        public void Reset()
        {
            foreach (var neuron in Neurons)
            {
                neuron.Updated = false;
                foreach(var delta in neuron.Deltas)
                    delta.X = 0.0;
            }
        }

        public void SetBias(NeuronBias bias)
        {
            Bias = bias;
            Neurons.Add(bias);
        }
        
        public List<Value> GetValues()
        {
            var values = new List<Value>();
            foreach (var neuron in Neurons)
                neuron.GetInputValues(values);
            return values;
        }

        public List<Value> GetDeltas()
        {
            var deltas = new List<Value>();
            foreach (var neuron in Neurons)
                neuron.GetInputDeltas(deltas);
            return deltas;
        }

        public void BackPropagate()
        {
            foreach (var neuron in Neurons)
                neuron.BackPropagate();
        }
    }
        
    public class NeuralNetwork
    {
        public NeuralLayer _inputs;
        public NeuralLayer _outputs;
        public List<NeuralLayer> _innerLayers = new List<NeuralLayer>();

        double _totalError = -1.0;
        public double Error { get { return _totalError; } }
        double _learningRate = -1.0;
        public double LearningRatePct { get { return _learningRate * 100.0; } }
        List<decimal> _weights;
        public List<decimal> Weights { get { return _weights; } }

        public NeuralNetwork(int nbInputs, int nbOuputs, List<int> innerLayerSizes)
        {
            _inputs = new NeuralLayer(nbInputs);
            _outputs = new NeuralLayer(nbOuputs);

            for (int idxLayer = 0; idxLayer < innerLayerSizes.Count; idxLayer++)
                _innerLayers.Add(new NeuralLayer(innerLayerSizes[idxLayer]));
             
            NeuralLayer prevLayer = _outputs;
            for (int idxLayer = innerLayerSizes.Count - 1; idxLayer >= 0 ; idxLayer--)
            {
                NeuralLayer postLayer = idxLayer > 0 ? _innerLayers[idxLayer - 1] : _inputs;
                for (int idxNeuron = 0; idxNeuron < innerLayerSizes[idxLayer]; idxNeuron++)
                    _innerLayers[idxLayer].Neurons.Add(new Neuron(postLayer, prevLayer));
                _innerLayers[idxLayer].SetBias(new NeuronBias(prevLayer));
                prevLayer = _innerLayers[idxLayer];
            }

            for (int idxInput = 0; idxInput < nbInputs; idxInput++)
                _inputs.Neurons.Add(new NeuronInput(prevLayer));
            _inputs.SetBias(new NeuronBias(prevLayer));

            for (int idxOutput = 0; idxOutput < nbOuputs; idxOutput++)
                _outputs.Neurons.Add(new NeuronOutput(_innerLayers.Last()));    
        }

        public void CalculateOutput(List<double> inputValues)
        {
            if (_inputs.InputNeurons.Count != inputValues.Count)
                throw new ApplicationException("Input neuron number does not match the number of input values");

            // set updated flag to false to all neurons in the inner layers
            _inputs.Reset();
            foreach (var layer in _innerLayers)
                layer.Reset();
            _outputs.Reset();

            // set the input values
            for (int idxInput = 0; idxInput < _inputs.InputNeurons.Count; idxInput++)
                _inputs.InputNeurons[idxInput].Value.X = inputValues[idxInput];

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

        public void BackPropagate()
        {
            _outputs.BackPropagate();
            for (int idxlayer = _innerLayers.Count - 1; idxlayer >= 0; idxlayer--)
                _innerLayers[idxlayer].BackPropagate();
        }

        public void Train(List<List<double>> inputValues, List<List<double>> outputValues, double obj_error = 1e-5, double max_error = 1e-5)
        {
            if (inputValues.Count != outputValues.Count)
                throw new ApplicationException("Training set inputs and outputs must have the same size");
            // input normalization
            int nbInputs = inputValues[0].Count;
            NRealMatrix inputTable = new NRealMatrix(inputValues.Count, nbInputs);
            var maxValue = 0.0;
            for (int idxInputList = 0; idxInputList < inputValues.Count; idxInputList++)
            {
                for (int idxInput = 0; idxInput < inputValues[idxInputList].Count; idxInput++)
                {
                    if (Math.Abs(inputValues[idxInputList][idxInput]) > maxValue)
                        maxValue = Math.Abs(inputValues[idxInputList][idxInput]);
                }
            }
            for(int idxInputList = 0; idxInputList < inputValues.Count; idxInputList++){
                for(int idxInput = 0; idxInput < inputValues[idxInputList].Count; idxInput++)
                    inputTable[idxInputList, idxInput] = inputValues[idxInputList][idxInput] / maxValue;
            }

            // output normalization
            int nbOutputs = outputValues[0].Count;
            NRealMatrix objectiveTable = new NRealMatrix(outputValues.Count, nbOutputs);
            maxValue = 0.0;
            for (int idxOutputList = 0; idxOutputList < outputValues.Count; idxOutputList++)
            {
                for (int idxOutput = 0; idxOutput < outputValues[idxOutputList].Count; idxOutput++)
                {
                    if (Math.Abs(outputValues[idxOutputList][idxOutput]) > maxValue)
                        maxValue = Math.Abs(outputValues[idxOutputList][idxOutput]);
                }
            }
            for(int idxOutputList = 0; idxOutputList < outputValues.Count; idxOutputList++){
                for(int idxOutput = 0; idxOutput < outputValues[idxOutputList].Count; idxOutput++)
                    objectiveTable[idxOutputList, idxOutput] = outputValues[idxOutputList][idxOutput] / maxValue;
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

            List<Value> modelParams = new List<Value>();
            foreach (var layer in _innerLayers)
            {
                foreach (var neuron in layer.Neurons)
                    modelParams.AddRange(neuron.Weights);
            }
            foreach (var neuron in _outputs.Neurons)
                modelParams.AddRange(neuron.Weights);

            List<Value> modelValues = new List<Value>();
            foreach (var layer in _innerLayers)
                modelValues.AddRange(layer.GetValues());
            modelValues.AddRange(_outputs.GetValues());
            
            List<Value> modelDeltas = new List<Value>();
            foreach (var layer in _innerLayers)
                modelDeltas.AddRange(layer.GetDeltas());
            modelDeltas.AddRange(_outputs.GetDeltas());
                                                        
            LevenbergMarquardt.model_func modelFunc = (NRealMatrix x, NRealMatrix weights) =>
            {
                // apply new weights
                for (int idxWeight = 0; idxWeight < weights.Columns; idxWeight++)
                    modelParams[idxWeight].X = weights[0, idxWeight];
                NRealMatrix y = new NRealMatrix(x.Rows, nbOutputs);
                // foreach set of input data                              
                for (int idxRow = 0; idxRow < x.Rows; idxRow++)
                {
                    // compute the ouput results
                    CalculateOutput(inputValues[Convert.ToInt32(x[idxRow, 0])]);                    
                    List<double> modelOutputs = GetOutput();
                    for (int idxCol = 0; idxCol < nbOutputs; idxCol++)
                        y[idxRow, idxCol] = modelOutputs[idxCol];
                }
                return y; 
            };

            LevenbergMarquardt.model_func jacFunc = (NRealMatrix x, NRealMatrix weights) =>
            {
                // apply new weights
                for (int idxWeight = 0; idxWeight < weights.Columns; idxWeight++)
                    modelParams[idxWeight].X = weights[0, idxWeight];
                // compute the jacobian matrix
                NRealMatrix jac = new NRealMatrix(x.Rows, weights.Columns);
                for (int idxRow = 0; idxRow < x.Rows; idxRow++)
                {
                    // compute the ouput results
                    CalculateOutput(inputValues[Convert.ToInt32(x[idxRow, 0])]);
                    // backpropagate the delta
                    BackPropagate();
                    for (int idxVal = 0; idxVal < modelValues.Count; idxVal++)
                        jac[idxRow, idxVal] = -modelValues[idxVal].X * modelDeltas[idxVal].X;
                }
                return jac; 
            };

            LevenbergMarquardt optimizer = new LevenbergMarquardt(objFunc, inputs, modelParams, modelFunc, jacFunc, 0.001, obj_error);
            try
            {
                optimizer.Solve();               
            }
            catch (StallException)
            {
                if (optimizer.Error > max_error)
                    throw;                
            }
            _totalError = optimizer.Error;
            _learningRate = (optimizer.StartError - optimizer.Error) / optimizer.StartError;
            _weights = modelParams.Select(param => (decimal)param.X).ToList();
        }
    }    
}
