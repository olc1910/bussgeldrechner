using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bussgeldrechner
{
    internal class Program
    {
        private const double ToleranzProzent = 0.03;
        private const int ToleranzZahl = 3;

        static void Main(string[] args)
        {
        }

        private static int calculate(int speed, int max_speed, bool toleranz, bool innerorts)
        {
            if (!toleranz) return max_speed - speed;

            if (toleranz && (innerorts || speed < 100)) return max_speed - speed - ToleranzZahl;

            speed -= speed * ToleranzProzent;
            return max_speed - speed;
        }

    }
}
