using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Diagnostics;

namespace Pong
{
    class GameObject
    {
        public Vector2 position = new Vector2(0, 0);
        public Vector2 newPositon = new Vector2(0, 0);
        public bool visible = false;

        protected Shape renderShape;
        protected Canvas canvas;

        public GameObject() { }
        public GameObject(Vector2 pos, Shape shape, Canvas cnvs)
        {
            position = pos;
            newPositon = position;
            canvas = cnvs;
            renderShape = shape;
        }


        public virtual void UpdatePos()
        {
            Canvas.SetLeft(renderShape, position.x);
            Canvas.SetTop(renderShape, position.y);
        }
        public virtual void UpdatePosLerp()
        {
            position.x = UnMath.Lerp(position.x, newPositon.x, 0.5d);
            position.y = UnMath.Lerp(position.y, newPositon.y, 0.5d);
            Canvas.SetLeft(renderShape, position.x);
            Canvas.SetTop(renderShape, position.y);
        }

        public virtual void UpdateVisibility()
        {
            renderShape.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        public virtual void SetVisibility(bool vis)
        {
            visible = vis;
            renderShape.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        public virtual Visibility GetVisibility()
        {
            return renderShape.Visibility;
        }
    }
}
