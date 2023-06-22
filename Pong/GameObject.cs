using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pong
{
    class GameObject
    {
        public Vector2 position = new Vector2(0, 0);

        protected Shape renderShape;
        protected Canvas canvas;

        public GameObject() { }
        public GameObject(Vector2 pos, Shape shape, Canvas cnvs)
        {
            position = pos;
            canvas = cnvs;
            renderShape = shape;
        }


        public virtual void UpdatePos()
        {
            Canvas.SetLeft(renderShape, position.x);
            Canvas.SetTop(renderShape, position.y);
        }
    }
}
