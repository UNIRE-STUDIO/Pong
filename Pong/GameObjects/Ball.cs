using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Pong
{
    class Ball : GameObject
    {
        public double widthBall = 15;
        private static SolidColorBrush solidColorBall = Brushes.WhiteSmoke;

        public Vector2 direction = new Vector2(1, 1);
        public int Speed { get; set; } = 2;

        public Ball(Vector2 pos, Shape shape, Canvas cnvs) : base(pos, shape, cnvs) 
        {
            renderShape.Visibility = Visibility.Hidden;
            renderShape.Width = widthBall;
            renderShape.Height = widthBall;
            renderShape.Fill = solidColorBall;
            canvas.Children.Add(renderShape);
            UpdatePos();
        }

        public void Calculation()
        {
            
        }

    }
}
