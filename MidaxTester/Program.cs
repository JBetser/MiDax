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
            // if there is a first argument "-G" then generate new results, otherwise test against the existing ones
            bool quick_test = (args.Length == 1 && args[0] == "-Q");
            bool generate = (args.Length >= 1 && args[0] == "-G");
            bool generate_to_db = ((args.Length == 2 && generate && args[1] == "-TODB") ||
                                    (args.Length == 3 && generate && (args[1] == "-TODB" || args[2] == "-TODB")));
            bool generate_from_db = ((args.Length == 2 && generate && args[1] == "-FROMDB") ||
                                    (args.Length == 3 && generate && (args[1] == "-FROMDB" || args[2] == "-FROMDB")));
            // test core functionalities
            if (!generate_to_db)
                Core.Run(generate, generate_from_db);

            List<DateTime> tmp = new List<DateTime>();
            tmp.Add(new DateTime(2016, 1, 20));
            tmp.Add(new DateTime(2016, 1, 21));
            tmp.Add(new DateTime(2016, 1, 22));
            Heuristic.Run(tmp, generate, generate_from_db, generate_to_db);
            
            /*
            if (!quick_test)
            {
                // test whole daily trading batches
                List<DateTime> tests = new List<DateTime>();
                tests.Add(new DateTime(2016, 1, 22));

                MacD.Run(tests, generate, generate_from_db, generate_to_db);
                Heuristic.Run(tests, generate, generate_from_db, generate_to_db);
                ANN.Run(tests, generate, generate_from_db, generate_to_db);
            }*/

            string statusSuccess = generate ? "Tests generated successfully" : "Tests passed successfully";
            Console.WriteLine(statusSuccess);

            MessageBox.Show(statusSuccess);
            
        }        
    }
}
