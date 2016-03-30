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
            bool quick_test = false;
            bool generate = false;
            bool generate_to_db = false;
            bool generate_from_db = false;
            bool heuristic = true;
            bool ann = true;
            bool use_uat_db = false;
            bool fullday = false;
            string date = "";
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-Q":
                        quick_test = true;
                        break;
                    case "-G":
                        generate = true;
                        break;
                    case "-FULL":
                        fullday = true;
                        break;
                    case "-FROMDB":
                        generate_from_db = true;
                        break;
                    case "-TODB":
                        generate_to_db = true;
                        break;
                    case "-UAT":
                        use_uat_db = true;
                        break;
                    case "-Heuristic":
                        ann = false;
                        break;
                    case "-ANN":
                        heuristic = false;
                        break;                    
                }
                if (arg.StartsWith("-DATE"))
                    date = arg.Substring(5);
            }
            // test core functionalities
            if (!generate_to_db && heuristic && ann)
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
                    tests.Add(new DateTime(2016, 2, 25));
                else
                    tests.Add(DateTime.Parse(date));

                if (heuristic)
                    Heuristic.Run(tests, generate, generate_from_db, generate_to_db, use_uat_db, fullday);
                if (ann)
                    ANN.Run(tests, generate, generate_from_db, generate_to_db, use_uat_db, fullday);
            }

            string statusSuccess = generate ? "Tests generated successfully" : "Tests passed successfully";
            Console.WriteLine(statusSuccess);

            if (!generate_to_db)
                MessageBox.Show(statusSuccess);            
        }        
    }
}
