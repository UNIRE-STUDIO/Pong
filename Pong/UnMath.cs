using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    public static class UnMath
    {
        public static double Lerp(double start, double end, double t)
        {
            return start * (1d - t) + end * t;
        }
    }
}
