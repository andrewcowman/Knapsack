using System;
using System.Linq;

namespace AnnealingKnapsack {
    public class Knapsack {

        public int maxWeightKnapsack; // maximum total weight of the knapsack
        public int maxWeightItem; // maximum weight of individual items
        public int maxValueItem; // maximum value of individual items
        public const int NumItemsToChoose = 50; // number of items to choose from

        private Random _rand = new Random(); // random object

        private int _maxIterations; // number of iterations to go through
        private int _currIteration; // current iteration
        private double _startTemp; // starting temperature for simulated annealing
        private double _currentTemp; // current temperature for simulated annealing
        private double _endTemp; // ending temperature for simulated annealing
        private bool _done = false; // whether process is done or not

        private double _currentScore; // current score (value of items in knapsack)
        private double _bestScore = double.MinValue; // best score

        private double _lastProbability; // probability of improving the score

        private int _cycles = 100; // number of cycles in an iteration

        private bool[] _currentTaken; // current items taken
        private bool[] _backupTaken; // backup of current items taken
        private bool[] _bestTaken; // best catalog of items taken so far

        private int[] _weights = new int[NumItemsToChoose + 1]; // weight of each item
        private int[] _values = new int[NumItemsToChoose + 1]; // value of each item

        /// <summary>
        /// Constructor.
        /// Calls separate constructor with max iterations and temperatures.
        /// </summary>
        public Knapsack() :
            this(100, 40000, 0.001) {

            Console.Write("Max weight of knapsack: ");
            maxWeightKnapsack = Convert.ToInt32(Console.ReadLine());
            Console.Write("Max weight of item: ");
            maxWeightItem = Convert.ToInt32(Console.ReadLine());
            Console.Write("Max value of item: ");
            maxValueItem = Convert.ToInt32(Console.ReadLine());

            // initialize
            _currentTaken = new bool[NumItemsToChoose];
            _backupTaken = new bool[NumItemsToChoose];
            _bestTaken = new bool[NumItemsToChoose];

            // randomly select items
            for(int i = 0; i < _currentTaken.Length; i++) {
                _currentTaken[i] = RandomBool();
            }

            // create random values and weights
            for(int i = 0; i < NumItemsToChoose; i++) {
                _weights[i] = _rand.Next(0, maxWeightItem);
                _values[i] = _rand.Next(0, maxValueItem);
            }

            WeighLess(); // remove item if over weight limit
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxIterations">Maximum number of iterations.</param>
        /// <param name="startTemp">Starting temperature for annealing process.</param>
        /// <param name="endTemp">Ending temperature for annealing process.</param>
        public Knapsack(int maxIterations, double startTemp, double endTemp) {
            _maxIterations = maxIterations;
            _startTemp = startTemp;
            _endTemp = endTemp;
        }

        /// <summary>
        /// Entry point of the program.
        /// </summary>
        public void Run() {

            while(!_done) { // while not done
                Iteration(); // run iteration method

                //display stuff
                Console.WriteLine("Iteration #" + _currIteration + " Best Value:" + _bestScore);
            }

            //display item table
            Console.WriteLine("item" + "\t" + "value" + "\t" + "weight" + "\t" + "taking");
            for (int n = 0; n < NumItemsToChoose; n++) {
                Console.WriteLine((n + 1) + "\t" + _values[n] + "\t" + _weights[n] + "\t" + _bestTaken[n]);
            }
        }

        /// <summary>
        /// Run an iteration.
        /// </summary>
        private void Iteration() {

            if(_currIteration == 0) { // first iteration
                _currentScore = Evaluate(); // set initial score
                Array.Copy(_currentTaken, _bestTaken, _currentTaken.Length); // create initial best array
                _bestScore = _currentScore; // set initial best score
            }

            _currIteration++; // increment iterations

            _currentTemp = Cooling(); // simulate cooling process

            // perform certain number of cycles during each iteration
            for(int cycle = 0; cycle < _cycles; cycle++) {

                Array.Copy(_currentTaken, _backupTaken, _currentTaken.Length); // create backup of current

                Randomize(); // randomize the items taken

                double cycleScore = Evaluate(); // get score for cycle

                bool improvement = false; // initialize improvement

                if(cycleScore > _currentScore) { // if cyclescore is greater than current score
                    improvement = true; // then we had an improvement
                } else {
                    // get probability of improvement
                    _lastProbability = Math.Exp(-(Math.Abs(cycleScore - _currentScore) / _currentTemp));

                    double d = _rand.NextDouble(); // random number

                    if(_lastProbability > d) { // if probability > random number
                        improvement = true; // allow for score to go backwards
                    }
                }

                if(improvement) { // if improvement

                    _currentScore = cycleScore; // set current = cycle
                    
                    if(cycleScore > _bestScore) { // if cycle score > best score

                        _bestScore = cycleScore; // set best = cycle

                        // move current taken to best taken
                        Array.Copy(_currentTaken, _bestTaken, _currentTaken.Length);

                    } else { // no improvement

                        // move backup into current
                        Array.Copy(_backupTaken, _currentTaken, _currentTaken.Length);

                    }
                }
            }

            // check to see if done
            if(_currIteration >= _maxIterations) {
                _done = true;
            }
        }

        /// <summary>
        /// Calculate the score of taken items.
        /// </summary>
        /// <returns></returns>
        private double Evaluate() {

            // if taken weight > weight of knapsack
            if(GetTotalWeight() > maxWeightKnapsack) {
                return 0; // return score of 0
            }

            int sum = 0; // init sum

            // for each current item
            for(int i = 0; i < _currentTaken.Length; i++) {
                if(_currentTaken[i]) { // if being taken
                    sum += _values[i]; // add to sum
                }
            }

            return sum; // return
        }

        /// <summary>
        /// Randomizes the items taken by adding one more item to knapsack,
        /// then balances the weight.
        /// </summary>
        private void Randomize() {

            // make sure it doesn't go into an infinite loop
            bool holdingEverything = _currentTaken.All(aCurrentTaken => aCurrentTaken);

            if(!holdingEverything) { // prevent infinite loop

                int i = _rand.Next(0, _currentTaken.Length); // random index

                // find one that isn't being taken
                while (_currentTaken[i]) {
                    i = _rand.Next(0, _currentTaken.Length);
                }

                _currentTaken[i] = true; // take random item

                WeighLess(); // balance knapsack
            }

        }

        /// <summary>
        /// Cooling schedule.
        /// </summary>
        /// <returns>New current temperature.</returns>
        private double Cooling() {
            double exp = (double)_currIteration / _maxIterations; // exponent for equation
            return _startTemp * Math.Pow(_endTemp/_startTemp, exp); // equation
        }

        /// <summary>
        /// Drop a random item from the items that are taken.
        /// </summary>
        private void WeighLess() {

            // while weight of taken items > max knapsack weight
            while (GetTotalWeight() > maxWeightKnapsack) {
                int idx = _rand.Next(0, _currentTaken.Length); // pick random index
                _currentTaken[idx] = false; // drop random index
            }

        }

        /// <summary>
        /// Get total weight of all items taken.
        /// </summary>
        /// <returns>Weight of items in knapsack.</returns>
        private int GetTotalWeight() {
            int sum = 0;
            for(int i = 0; i < _currentTaken.Length; i++) {
                if(_currentTaken[i]) {
                    sum += _weights[i];
                }
            }
            return sum;
        }

        /// <summary>
        /// Random boolean generator.
        /// </summary>
        /// <returns>Random boolean value.</returns>
        private bool RandomBool() {
            return (_rand.Next(0, 2) == 0) ? true : false;
        }
    }
}
