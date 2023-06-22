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
    class Paddle : GameObject
    {
        public Paddle(Vector2 pos, Shape shape, Canvas cnvs) : base(pos, shape, cnvs) { }

        public Vector2 Direction { get; set; } = new Vector2(0, 0);
        public int Speed { get; set; } = 3;

        public void Calculation()
        {
            position.x = position.x + Direction.x * Speed;
            position.y = position.y + Direction.y * Speed;
            if (position.y < 0) position.y = 0;
            if (position.y > canvas.Height - renderShape.Height) position.y = (int)canvas.Height - (int)renderShape.Height;
        }
    }
}
