using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pong
{
    class InputManager
    {
        public bool keyUpIsPressed;
        public bool keyDownIsPressed;

        public EventHandler pauseClick;

        public void OnKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Up || e.Key == Key.W)
            {
                keyUpIsPressed = true;
            }
           
            if (e.Key == Key.Down || e.Key == Key.S)
            {
                keyDownIsPressed = true;
            }

            if (e.Key == Key.Space)
            {
                pauseClick?.Invoke(this, e);
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Up || e.Key == Key.W)
            {
                keyUpIsPressed = false;
            }

            if (e.Key == Key.Down || e.Key == Key.S)
            {
                keyDownIsPressed = false;
            }
        }
    }
}
