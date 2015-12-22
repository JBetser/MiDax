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
    public class Value
    {
        public double X = 0.0;

        public Value(double val = 0.0)
        {
            X = val;
        }
    }

    public class StallException : ApplicationException
    {
        public StallException() : base("LevenbergMarquardt is in a stall") { }
    }

    public class LevenbergMarquardt
    {
        public delegate NRealMatrix objective_func(NRealMatrix input);
        public delegate NRealMatrix model_func(NRealMatrix input, NRealMatrix weights);
        
        objective_func _obj_func;
        model_func _model;
        model_func _jac_func;
        NRealMatrix _inputs;
        NRealMatrix _outputs;
        NRealMatrix _weights;
        NRealMatrix _jac;
        NRealMatrix _error;
        double _obj_error;
        double _totalError;
        double _lambda;
        int _max_iter;
        int _retry = 0;
        List<Value> _modelParams;  // The results are updated here

        public double ObjectiveError { get { return _obj_error; } }
        public double Error { get { return _totalError; } }

        public LevenbergMarquardt(objective_func obj_func, List<double> inputs, List<Value> modelParams, model_func model, model_func model_jac, double lambda = 0.001, double obj_error = 0.00001, int max_iter = 10000)
        {
            if (inputs.Count == 0)
                throw new ApplicationException("Number of input data must be > 0");
            if (modelParams.Count == 0)
                throw new ApplicationException("Number of model parameters must be > 0");
            _obj_func = obj_func;
            _model = model;
            _jac_func = model_jac;
            _lambda = lambda;
            _max_iter = max_iter;
            _inputs = new NRealMatrix(inputs.Count, 1);
            _inputs.SetArray((from input in inputs select new NDouble[] { new NDouble(input) } ).ToArray());
            _outputs = _obj_func(_inputs);
            _modelParams = modelParams;

            // initalize the weights with normal random distibution
            var seed = new MLapack.MCJIMatrix(4,1);
            seed.setAt(0, 0, 321);
            seed.setAt(1, 0, 321);
            seed.setAt(2, 0, 321);
            seed.setAt(3, 0, 321);

            // check if a guess has been provided
            bool modelParamInitialized = false;
            foreach (var weight in modelParams)
            {
                if (weight.X != 0)
                    modelParamInitialized = true;
            }
            if (modelParamInitialized)
            {
                _weights = new NRealMatrix(1, modelParams.Count);
                _weights.SetArray(new NDouble[][] {(from param in modelParams select new NDouble(param.X)).ToArray() });
            }
            else
            {
                _weights = LapackLib.Instance.RandomMatrix(RandomDistributionType.Uniform_0_1, seed, 1, modelParams.Count);
                for (int idxWeight = 0; idxWeight < _weights.Columns; idxWeight++)
                    _weights[0, idxWeight] = (_weights[0, idxWeight] * 2.0 - 1.0) / Math.Sqrt(inputs.Count);
            }

            _obj_error = obj_error;
            _error = calcError(_weights);
            _totalError = calcTotalError(_error);
        }
        
        public void Solve()
        {
            int nbIter = 0;
            while (_totalError > _obj_error)
            {
                if (nbIter++ > _max_iter)
                    throw new ApplicationException("LevenbergMarquardt cannot converge within maximum number of iterations"); 
                
                nextStep();
            }
            // update the weights
            for (int idxWeight = 0; idxWeight < _weights.Columns; idxWeight++)
                _modelParams[idxWeight].X = _weights[0, idxWeight];
        }

        NRealMatrix calcError(NRealMatrix weights)
        {
            var error = new NRealMatrix(_inputs.Rows, 1);
            NRealMatrix modelOutput = _model(_inputs, weights);
            for (int idxError = 0; idxError < error.Rows; idxError++)
                error.SetAt(idxError, 0, new NDouble(_outputs[idxError, 0] - modelOutput[idxError, 0]));
            return error;
        }

        double calcTotalError(NRealMatrix error)
        {
            double sumSquare = 0;
            for (int idxError = 0; idxError < error.Rows; idxError++)
                sumSquare += error[idxError, 0] * error[idxError, 0];
            return Math.Sqrt(sumSquare);
        }
        
        void nextStep()
        {
            // compute jacobian
            if (_retry == 0)
                _jac = _jac_func(_inputs, _weights);
            // compute hessian approximation with tykhonov damping coefficient
            var jacT = new NRealMatrix(_jac.Rows, _jac.Columns);
            jacT.SetArray(_jac.ToArray());
            jacT.Transpose();
            var dampedHessian = new NRealMatrix(_jac.Columns, _jac.Columns);
            dampedHessian = jacT * _jac;
            for (int idxRow = 0; idxRow < dampedHessian.Rows; idxRow++)
                dampedHessian.SetAt(idxRow, idxRow, new NDouble(dampedHessian[idxRow, idxRow] * (1.0 + _lambda) + 1e-10));
            var adj = new NRealMatrix(dampedHessian.Rows, 1);
            var y = new NRealMatrix(dampedHessian.Rows, 1);
            y = jacT * _error;
            // solve dampedHessian * adj = y
            LapackLib.Instance.SolveSle(dampedHessian, y, adj);
            var nextWeights = new NRealMatrix(1, _weights.Columns);
            for (int idxWeight = 0; idxWeight < nextWeights.Columns; idxWeight++)
                nextWeights.SetAt(0, idxWeight, new NDouble(_weights[0, idxWeight] - adj[idxWeight, 0]));
            // compute errors
            var error = calcError(nextWeights);
            var totalError = calcTotalError(error);
            if (totalError > _totalError)
            {
                // revert step and increase damping factor                
                if (_retry < 100)
                {
                    _lambda *= 11.0;
                    _retry++;
                }
                else
                {
                    throw new StallException(); 
                }
            }
            else
            {
                // accept step and decrease damping factor
                _lambda /= 9.0;
                _weights.SetArray(nextWeights.ToArray());
                _error = error;
                _totalError = totalError;
                _retry = 0;
            }
        }

    }
}
