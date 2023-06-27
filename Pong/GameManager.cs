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
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pong
{
    class GameManager
    {
        Canvas canvas;
        Rectangle rectLocal;
        Rectangle rectOpponent;
        
        Paddle paddleLocal;
        Paddle paddleOpponent;
        List<Ball> balls = new List<Ball>();
        List<Ball> activeBalls = new List<Ball>();

        MainWindow mainWindow;
        InputManager inputManager;

        NetworkData networkSendData = new NetworkData();
        NetworkData networkReceiveData = new NetworkData();

        public bool isHost = false;
        public bool isConnect = false;
        bool isPause = true;
        public bool isActiveGameLoop = false;

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
                        networkSendData.dataDictionary[(char)Keys.leftSideScore] = leftSideScore.ToString();
                    }
                    else
                    {
                        networkSendData.dataDictionary.Add((char)Keys.leftSideScore, leftSideScore.ToString());
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
                        networkSendData.dataDictionary[(char)Keys.rightSideScore] = rightSideScore.ToString();
                    }
                    else
                    {
                        networkSendData.dataDictionary.Add((char)Keys.rightSideScore, rightSideScore.ToString());
                    }
                }
            }
        }

        public int fps;

        // Сервер
        DispatcherTimer timerAfterGoal;
        int delayAfterGoal = 1;
        Random rand = new Random();


        public GameManager(MainWindow mainWind, InputManager inputM)
        {
            mainWindow = mainWind;
            inputManager = inputM;

            inputManager.pauseClick += (sender, e) => PauseIsActive();

            canvas = mainWind.canvas;
            rectLocal = mainWind.rectLocal;
            rectOpponent = mainWind.rectOpponent;
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
                        return 1; // Даём понять обработчику у кого произошла ошибка
                    }
                    else
                    {
                        return 2; //
                    }
                }
                
                if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.paddlePosY, out string y))
                {
                    paddleOpponent.position = new Vector2(paddleOpponent.position.x, int.Parse(y));
                }

                if (!isHost)
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        if (networkReceiveData.dataDictionary.TryGetValue((char)(i-1), out string vec))
                        {
                            Vector2 pos = new Vector2();
                            pos.SetString(vec);
                            balls[i].position = pos;
                            balls[i].visible = true;
                            activeBalls.Add(balls[i]);
                        }
                        else
                        {
                            balls[i].visible = false;
                            activeBalls.Remove(balls[i]);
                        }
                    }
                    
                    if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.leftSideScore, out string left))
                    {
                        LeftSideScore = int.Parse(left);
                    }
                    
                    if (networkReceiveData.dataDictionary.TryGetValue((char)Keys.rightSideScore, out string right))
                    {
                        RightSideScore = int.Parse(right);
                    }
                }
                await Task.Delay(10);
            }
            return errorId;
        }

        public async void SendPos(UnTcpSocket socket)
        {
            while (socket.isConnect)
            {
                networkSendData.dataDictionary.Add((char)Keys.paddlePosY, ((int)paddleLocal.position.y).ToString());
                if (isHost)
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        if (balls[i].GetVisibility() != Visibility.Visible) continue;
                        networkSendData.dataDictionary.Add((char)(i+1), balls[i].position.ToString());
                    }
                }
                socket.Send(networkSendData.ToString());
                networkSendData.dataDictionary.Clear();
                await Task.Delay(20);
            }
        }

        // GAME LOOOOOOOOOOOP
        public void GameSetUp()
        {
            rectLocal.Visibility = Visibility.Visible;
            rectOpponent.Visibility = Visibility.Visible;
            int xPos = 0;
            if (isHost)
            {
                isPause = true;
                xPos = (int)canvas.Width - (int)rectLocal.Width;
                paddleLocal = new Paddle(new Vector2(xPos - 1, canvas.Height/2), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(1, canvas.Height / 2), rectOpponent, canvas);

                timerAfterGoal = new DispatcherTimer();
                timerAfterGoal.Interval = TimeSpan.FromSeconds(delayAfterGoal);
            }
            else
            {
                xPos = (int)canvas.Width - (int)rectOpponent.Width;
                paddleLocal = new Paddle(new Vector2(1, canvas.Height / 2), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(xPos - 1, canvas.Height / 2), rectOpponent, canvas);
            }
            if (balls.Count == 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Ball newBall = new Ball(new Vector2((int)canvas.Width / 2, 50), new Ellipse(), canvas);
                    balls.Add(newBall);
                }
            }
            balls[0].visible = true;
            balls[0].SetVisibility(true); // Включаем видимость лишь первого мячика
            activeBalls.Add(balls[0]);

            isActiveGameLoop = true;
            GamerLoop();
        }

        const int ms_per_update = 16;
        private async void GamerLoop()
        {
            DateTimeOffset n = (DateTimeOffset)DateTime.UtcNow;

            int elapsed = 0;
            long currentTime = 0;
            long pervious = n.ToUnixTimeMilliseconds();
            int lag = 0;

            while (isActiveGameLoop)
            {
                DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
                currentTime = now.ToUnixTimeMilliseconds();
                elapsed = (int)(currentTime - pervious); // Время между предыдущим и текущим кадром
                pervious = currentTime;             // Сохраняем время текущего кадра
                lag += elapsed;                     // Суммированное время между кадрами

                // Сохраняем лаг, т.е время с предыдущего РАБОЧЕГО кадра (для подсчета ФПС)
                // Так-как потом мы изменяем glManager.lag
                int curLag = lag;
                
                Update();
                lag -= elapsed;
                /*
                // При накоплении лагов, змейка начнёт отставать на несколько итераций т.е перемещений
                // с помощью этого цикла мы нагоняем змейку к её нужному положению
                */
                
                while (lag >= ms_per_update)
                {
                    Update();
                    lag -= ms_per_update;
                }

                if (curLag != 0) fps = 1000 / curLag;

                await Task.Delay(1);
            }
        }

        // Потом надо убрать асинхронность
        private void Update()
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
                foreach (Ball ball in balls)
                {
                    // Нельзя изменять массив, который перебираем, поэтому проверяют вот так:
                    // Возможно стоит добавлять или удалять элементы после цикла.
                    if (ball.GetVisibility() != Visibility.Visible) continue;

                    ball.position.x = ball.position.x + ball.direction.x * ball.Speed;
                    ball.position.y = ball.position.y + ball.direction.y * ball.Speed;

                    // Касаемся ракеток
                    // Левая ракетка
                    if (ball.position.x < rectLocal.Width &&
                        ball.position.y > paddleOpponent.position.y - ball.widthBall &&
                        ball.position.y < paddleOpponent.position.y + rectOpponent.Height)
                    {
                        

                        ball.position.x = rectLocal.Width;
                        Vector2 oldDir = ball.direction;
                        ball.direction = NewReflectionVector(ball.direction);
                        Trace.WriteLine($"x{ball.direction.x},y{ball.direction.y}");

                        Ball newBall = GetDisabledBall();
                        newBall.position = ball.position;
                        newBall.direction = NewReflectionVector(oldDir);
                        newBall.direction.y = -newBall.direction.y;
                        newBall.SetVisibility(true);
                        activeBalls.Add(newBall);
                    }
                    // Правая ракетка
                    else if (ball.position.x > canvas.Width - rectLocal.Width - ball.widthBall &&
                             ball.position.y > paddleLocal.position.y - ball.widthBall &&
                             ball.position.y < paddleLocal.position.y + rectLocal.Height)
                    {
                        ball.position.x = canvas.Width - rectLocal.Width - ball.widthBall;
                        Vector2 oldDir = ball.direction;
                        ball.direction = NewReflectionVector(ball.direction);
                        Trace.WriteLine($"x{ball.direction.x},y{ball.direction.y}");

                        Ball newBall = GetDisabledBall();
                        newBall.position = ball.position;
                        newBall.direction = NewReflectionVector(oldDir);
                        newBall.direction.y = -newBall.direction.y;
                        newBall.SetVisibility(true);
                        activeBalls.Add(newBall);
                    }

                    if (ball.position.y >= canvas.Height - ball.widthBall ||
                        ball.position.y <= 0)
                    {
                        ball.direction.y = -ball.direction.y;
                    }

                    if (ball.position.x <= 0)
                    {
                        RightSideScore++;
                        Goal(ball);
                    }
                    else if (ball.position.x + ball.widthBall > canvas.Width)
                    {
                        LeftSideScore++;
                        Goal(ball);
                    }

                    ball.UpdatePos();
                }
            }

            if (!isHost)
            {
                mainWindow.leftSideScore.Content = LeftSideScore.ToString();
                mainWindow.rightSideScore.Content = RightSideScore.ToString();

                foreach (Ball ball in balls)
                {
                    ball.UpdateVisibility();
                    if (!ball.visible) continue;
                    ball.UpdatePos();
                }
            }
        }

        private void Goal(Ball ball)
        {
            if (activeBalls.Count == 1)
            {
                Random rand = new Random();
                int dir = rand.Next(0, 4);
                switch (dir)
                {
                    case 0:
                        ball.direction.x = 1;
                        ball.direction.y = -1;
                        break;
                    case 1:
                        ball.direction.x = 1;
                        ball.direction.y = 1;
                        break;
                    case 2:
                        ball.direction.x = -1;
                        ball.direction.y = -1;
                        break;
                    case 3:
                        ball.direction.x = -1;
                        ball.direction.y = 1;
                        break;
                }
                ball.position = new Vector2(canvas.Width / 2, rand.Next(10, (int)(canvas.Height - ball.widthBall)));
            }
            else
            {
                activeBalls.Remove(ball);
                ball.SetVisibility(false);
            }
        }

        public void ResetGame()
        {
            LeftSideScore = 0;
            RightSideScore = 0;
            mainWindow.leftSideScore.Content = LeftSideScore.ToString();
            mainWindow.rightSideScore.Content = RightSideScore.ToString();

            activeBalls.Clear();
            foreach (Ball ball in balls)
            {
                ball.SetVisibility(false);
            }
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

        private Ball GetDisabledBall()
        {
            foreach (Ball ball in balls)
            {
                if (ball.GetVisibility() == Visibility.Hidden)
                {
                    return ball;
                }
            }
            MessageBox.Show("Предел количества мячей");
            return null;
        }

        private Vector2 NewReflectionVector(Vector2 dir)
        {
            double newDirY = (double)rand.Next(1, 11) / 10;

            // Мяч летит вниз
            if (dir.y < 0)
            {
                newDirY = -newDirY;
            }
            double newDirX = 1 + (1 - Math.Abs(newDirY));
            newDirX = dir.x < 0 ? newDirX : -newDirX;

            return new Vector2(newDirX, newDirY);
        }

    }
}
