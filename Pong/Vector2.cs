using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    struct Vector2
    {
        public double x;
        public double y;

        public Vector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"x{Math.Round(x,1)}y{Math.Round(y, 1)}";
        }

        public void SetString(string str)
        {
            StringBuilder strBuilder = new StringBuilder();
            Trace.WriteLine(str);
            for (int i = 1; i < str.Length; i++)
            {
                if (i == str.Length - 1)
                {
                    strBuilder.Append(str[i]);
                    y = double.Parse(strBuilder.ToString());
                }
                else if (str[i] == 'y')
                {
                    x = double.Parse(strBuilder.ToString());
                    strBuilder.Clear();
                }
                else
                {
                    strBuilder.Append(str[i]);
                }
            }
            Trace.WriteLine($"(x:{x} y:{y})");
        }
    }
}
