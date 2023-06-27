using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    class SettingsManager
    {
        MainWindow mainWindow;
        public bool isActive = true;
        public int sizeId = 0;

        private Vector2[] sizeWindow =
        {
            new Vector2(420,435),
            new Vector2(540,520)
        };

        private Vector2[] sizeCanvas =
        {
            new Vector2(390,330),
            new Vector2(490,400)
        };

        public SettingsManager (MainWindow window)
        {
            mainWindow = window;
        }

        public void ChangeSizeWindow(int id)
        {
            if (!isActive) return;

            mainWindow.Width = sizeWindow[id].x;
            mainWindow.Height = sizeWindow[id].y;

            mainWindow.canvas.Width = sizeCanvas[id].x;
            mainWindow.canvas.Height = sizeCanvas[id].y;
        }
    }
}
