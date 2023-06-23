using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pong
{
    class GameManager
    {
        Canvas canvas;
        Paddle paddleLocal;
        Paddle paddleOpponent;
        Ball ball;

        Rectangle rectLocal;
        Rectangle rectOpponent;
        Ellipse EllipseBall;

        MainWindow mainWindow;
        InputManager inputManager;

        NetworkData networkSendData = new NetworkData();
        NetworkData networkReceiveData = new NetworkData();

        public bool isHost = false;
        public bool isConnect = false;

        int leftSideScore = 0;
        int LeftSideScore
        {
            get { return leftSideScore; }
            set { leftSideScore = value;
                if (isHost)
                {
                    mainWindow.leftSideScore.Content = leftSideScore.ToString();
                    if (networkSendData.dataDictionary.TryGetValue((char)Keys.leftSideScore, out _))
                    {
                        networkSendData.dataDictionary[(char)Keys.leftSideScore] = leftSideScore;
                    }
                    else
                    {
                        networkSendData.dataDictionary.Add((char)Keys.leftSideScore, leftSideScore);
                    }
                }
            }
        }
        int rightSideScore = 0;
        int RightSideScore
        {
            get { return rightSideScore; }
            set {
                rightSideScore = value;
                if (isHost)
                {
                    mainWindow.rightSideScore.Content = rightSideScore.ToString();
                    if (networkSendData.dataDictionary.TryGetValue((char)Keys.rightSideScore, out _))
                    {
                        networkSendData.dataDictionary[(char)Keys.rightSideScore] = rightSideScore;
                    }
                    else
                    {
                        networkSendData.dataDictionary.Add((char)Keys.rightSideScore, rightSideScore);
                    }
                }
            }
        }

        public GameManager(MainWindow mainWind, InputManager inputM)
        {
            mainWindow = mainWind;
            inputManager = inputM;

            inputManager.pauseClick += (sender, e) => PauseIsActive();

            canvas = mainWind.canvas;
            rectLocal = mainWind.rectLocal;
            rectOpponent = mainWind.rectOpponent;
            EllipseBall = mainWind.EllipseBall;
        }

        public async Task<int> ReceivePos(UnTcpSocket socket)
        {
            int errorId = 0;
            while (socket.isConnect)
            {
                networkReceiveData.dataDictionary.Clear();

                try
                {
                    networkReceiveData.Unpacking(socket.Receive());
                }
                catch (Exception e)
                {
                    if (isHost)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
                
                if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.paddlePosY, out int y))
                {
                    paddleOpponent.position = new Vector2(paddleOpponent.position.x, y);
                }

                if (!isHost)
                {
                    if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.ballPosX, out int posX) &&
                        networkReceiveData.dataDictionary.TryGetValue((char)Keys.ballPosY, out int posY))
                    {
                        ball.position = new Vector2(posX, posY);
                    }
                        
                    if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.leftSideScore, out int left))
                    {
                        LeftSideScore = left;
                    }
                    
                    if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.rightSideScore, out int right))
                    {
                        RightSideScore = right;
                    }
                }
                await Task.Delay(30);
            }
            return errorId;
        }

        public async void SendPos(UnTcpSocket socket)
        {
            while (socket.isConnect)
            {
                networkSendData.dataDictionary.Add((char)Keys.paddlePosY, paddleLocal.position.y);
                if (isHost)
                {
                    networkSendData.dataDictionary.Add((char)Keys.ballPosX, ball.position.x);
                    networkSendData.dataDictionary.Add((char)Keys.ballPosY, ball.position.y);
                }
                socket.Send(networkSendData.ToString());
                networkSendData.dataDictionary.Clear();
                await Task.Delay(50);
            }
        }

        // GAME LOOOOOOOOOOOP
        public void GameSetUp()
        {
            rectLocal.Visibility = Visibility.Visible;
            rectOpponent.Visibility = Visibility.Visible;
            EllipseBall.Visibility = Visibility.Visible;
            int xPos = 0;
            if (isHost)
            {
                xPos = (int)canvas.Width - (int)rectLocal.Width;
                paddleLocal = new Paddle(new Vector2(xPos - 1, 110), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(1, 110), rectOpponent, canvas);
                ball = new Ball(new Vector2((int)canvas.Width / 2, 50), EllipseBall, canvas);
            }
            else
            {
                xPos = (int)canvas.Width - (int)rectOpponent.Width;
                paddleLocal = new Paddle(new Vector2(1, 110), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(xPos - 1, 110), rectOpponent, canvas);
                ball = new Ball(new Vector2((int)canvas.Width / 2, 50), EllipseBall, canvas);
            }
            isActiveGameLoop = true;
            GamerLoop();
        }

        bool isPause = true;
        bool isActiveGameLoop = false;
        private async void GamerLoop()
        {
            while (isActiveGameLoop)
            {
                int direction = 0;
                if (inputManager.keyDownIsPressed)
                {
                    direction = 1;
                }
                else if (inputManager.keyUpIsPressed)
                {
                    direction = -1;
                }
                paddleLocal.Direction = new Vector2(paddleLocal.Direction.x, direction);
                paddleLocal.Calculation();
                paddleLocal.UpdatePos();
                paddleOpponent.UpdatePos();

                // Поменять на BOOL IsServer
                if (isHost && !isPause)
                {
                    ball.position.x = ball.position.x + ball.Direction.x * ball.Speed;
                    ball.position.y = ball.position.y + ball.Direction.y * ball.Speed;

                    // Касаемся ракеток
                    if (ball.position.x < rectLocal.Width &&
                        ball.position.y > paddleOpponent.position.y - EllipseBall.Width &&
                        ball.position.y < paddleOpponent.position.y + rectOpponent.Height)
                    {
                        ball.position.x = (int)rectLocal.Width;
                        ball.Direction.x = -ball.Direction.x;
                    }
                    else if (ball.position.x > canvas.Width - rectLocal.Width - EllipseBall.Width &&
                             ball.position.y > paddleLocal.position.y - EllipseBall.Width &&
                             ball.position.y < paddleLocal.position.y + rectLocal.Height)
                    {
                        ball.position.x = (int)canvas.Width - (int)rectLocal.Width - (int)EllipseBall.Width;
                        ball.Direction.x = -ball.Direction.x;
                    }

                    if (ball.position.y >= canvas.Height - EllipseBall.Width ||
                        ball.position.y <= 0)
                    {
                        ball.Direction.y = -ball.Direction.y;
                    }

                    if (ball.position.x <= 0)
                    {
                        LeftSideScore++;
                        await Task.Delay(1000);
                        Goal();
                    }
                    else if (ball.position.x + EllipseBall.Width > canvas.Width)
                    {
                        RightSideScore++; 
                        await Task.Delay(1000);
                        Goal();
                    }
                }

                if (!isHost)
                {
                    mainWindow.leftSideScore.Content = LeftSideScore.ToString();
                    mainWindow.rightSideScore.Content = RightSideScore.ToString();
                }

                ball.UpdatePos();
                await Task.Delay(16);
            }
        }
        DispatcherTimer delayAfterGoal;
        private void Goal()
        {
            isPause = true;
            Random rand = new Random();
            int dir = rand.Next(0, 4);
            switch (dir)
            {
                case 0:
                    ball.Direction.x = 1;
                    ball.Direction.y = -1;
                    break;
                case 1:
                    ball.Direction.x = 1;
                    ball.Direction.y = 1;
                    break;
                case 2:
                    ball.Direction.x = -1;
                    ball.Direction.y = -1;
                    break;
                case 3:
                    ball.Direction.x = -1;
                    ball.Direction.y = 1;
                    break;
            }
            ball.position = new Vector2((int)canvas.Width / 2, rand.Next(0, (int)(canvas.Height - EllipseBall.Height)));
            isPause = false;
        }

        public void ResetGame()
        {
            LeftSideScore = 0;
            RightSideScore = 0;
            mainWindow.leftSideScore.Content = LeftSideScore.ToString();
            mainWindow.rightSideScore.Content = RightSideScore.ToString();
        }

        private void PauseIsActive()
        {
            // IS CONNECT
            isPause = !isPause;
        }

        public void StopGameLoop()
        {
            isActiveGameLoop = false;
        }
    }
}
