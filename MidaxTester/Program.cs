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
            bool generate_to_db = ((args.Length >= 2 && generate && args[1] == "-TODB") ||
                                    (args.Length >= 3 && generate && (args[1] == "-TODB" || args[2] == "-TODB")));
            bool generate_from_db = ((args.Length >= 2 && generate && args[1] == "-FROMDB") ||
                                    (args.Length >= 3 && generate && (args[1] == "-FROMDB" || args[2] == "-FROMDB")));
            string type = args.Length >= 4 ? args[3] : "";
            string date = args.Length >= 5 ? args[4] : "";
            // test core functionalities
            if (!generate_to_db)
                Core.Run(generate, generate_from_db);

            /*
            List<DateTime> tmp = new List<DateTime>();
            tmp.Add(new DateTime(2016, 1, 19));
            tmp.Add(new DateTime(2016, 1, 20));
            tmp.Add(new DateTime(2016, 1, 21));
            tmp.Add(new DateTime(2016, 1, 22));
            tmp.Add(new DateTime(2016, 1, 25));
            tmp.Add(new DateTime(2016, 1, 26));
            tmp.Add(new DateTime(2016, 1, 27));
            tmp.Add(new DateTime(2016, 1, 28));
            tmp.Add(new DateTime(2016, 1, 29));
            ANN.Run(tmp, generate, generate_from_db, generate_to_db);*/
            
            if (!quick_test)
            {
                // test whole daily trading batches
                List<DateTime> tests = new List<DateTime>();
                if (date == "")
                    tests.Add(new DateTime(2016, 1, 22));
                else
                    tests.Add(DateTime.Parse(date.Substring(1)));

                if (type == "" || type == "-Heuristic")
                    Heuristic.Run(tests, generate, generate_from_db, generate_to_db);
                if (type == "" || type == "-ANN")
                    ANN.Run(tests, generate, generate_from_db, generate_to_db);
            }

            string statusSuccess = generate ? "Tests generated successfully" : "Tests passed successfully";
            Console.WriteLine(statusSuccess);

            if (!generate_to_db)
                MessageBox.Show(statusSuccess);            
        }        
    }
}
