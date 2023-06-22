using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Pong
{
    class Ball : GameObject
    {
        public Vector2 Direction = new Vector2(1, 1);
        public int Speed { get; set; } = 3;

        public Ball(Vector2 pos, Shape shape, Canvas cnvs) : base(pos, shape, cnvs) { }

        public void Calculation()
        {
            
        }
    }
}
