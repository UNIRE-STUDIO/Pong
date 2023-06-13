using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pong
{
    class Paddle
    {
        public Vector2 position = new Vector2(0,0);
        public Vector2 size = new Vector2(10,60);

        public int Direction { get; set; } = 0;
        public int Speed { get; set; } = 3;

        private Rectangle rectangle;
        private Canvas canvas;

        public Paddle() { }
        public Paddle(Vector2 pos, Rectangle rect, Canvas cnvs)
        {
            position = pos;
            rectangle = rect;
            canvas = cnvs;
            Canvas.SetLeft(rectangle, position.x);
        }

        public void UpdateDirection()
        {
            position.y += Direction * Speed;
            UpdatePos();
        }

        public void UpdatePos()
        {
            if (position.y < 0) position.y = 0;
            if (position.y > canvas.Height - size.y) position.y = (int)canvas.Height - size.y;
            Canvas.SetTop(rectangle, position.y);
        }

        // Пока-что используем только Y
        public void SetPos(Vector2 newPos)
        {
            position.y = newPos.y;
        }
    }
}
