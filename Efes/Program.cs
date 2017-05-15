using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Efes
{
    class Program
    {
        static void Main(string[] args)
        {
            var dropper = new Dropper("keywords.dat", new System.Globalization.CultureInfo("en-US"));
            Console.ReadKey();
            dropper.Listening = false;
        }
    }
}
