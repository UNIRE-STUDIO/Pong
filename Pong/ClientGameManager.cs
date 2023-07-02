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
    class ClientGameManager
    {
        private Canvas canvas;
        private Rectangle rectLocal;
        private Rectangle rectOpponent;

        private Paddle paddleLocal;
        private Paddle paddleOpponent;
        private List<Ball> balls = new List<Ball>();
        private List<Ball> activeBalls = new List<Ball>();
        private MainWindow mainWindow;

        private InputManager inputManager;

        private ClientTcpSocket clientTcpSocket;
        private ClientUdpSocket clientUdpSocket;

        private NetworkData networkSendDataTcp = new NetworkData();
        private NetworkData networkSendDataUdp = new NetworkData();

        private NetworkData networkReceiveDataTcp = new NetworkData();
        private NetworkData networkReceiveDataUdp = new NetworkData();

        public bool isConnect = false;
        private bool isPause = true;
        public bool isActiveGameLoop = false;

        private int leftSideScore = 0;
        private int rightSideScore = 0;

        public int fps;

        private DispatcherTimer timerAfterGoal;
        private int delayAfterGoal = 1;
        private Random rand = new Random();

        public ClientGameManager(MainWindow mainWind, InputManager inputM)
        {
            mainWindow = mainWind;
            inputManager = inputM;

            inputManager.pauseClick += (sender, e) => PauseIsActive();

            canvas = mainWind.canvas;
            rectLocal = mainWind.rectLocal;
            rectOpponent = mainWind.rectOpponent;
        }

        public void Connect(string serverIp, int serverPort)
        {
            clientTcpSocket = new ClientTcpSocket();
            clientUdpSocket = new ClientUdpSocket();
            clientTcpSocket.eventStart += (handle, ee) =>
            {
                mainWindow.clentStatus.Content = "Подключение...";
            };
            clientTcpSocket.eventConnect += async (handle, ee) =>
            {
                mainWindow.clentStatus.Content = "Клиент: Подключение установлено";
                GameSetUp();
                await Task.Run(() => {
                    SendUdp(clientTcpSocket, clientUdpSocket);
                    ReceiveUdp(clientTcpSocket, clientUdpSocket);
                    ReceiveTcp(clientTcpSocket, clientUdpSocket);
                });
            };
            clientTcpSocket.eventErrorConnect += (ee, args) =>
            {
                // Вывести через делегаты
                MessageBox.Show("Клиент: Не удалось подключиться... " + ((Exception)ee).Message);
                mainWindow.Dispatcher.Invoke((Action)delegate
                {
                    mainWindow.DisconnectClient_Click();
                });
            };
            clientUdpSocket.eventErrorReceive += (hendler, ee) => { // Клиент разорвал соединение или что-то ещё
                mainWindow.Dispatcher.Invoke((Action)delegate
                {
                    mainWindow.DisconnectClient_Click();
                });
                MessageBox.Show("Клиент UDP: Не удалось получить данные");
            };
            clientTcpSocket.eventErrorReceive += (hendler, ee) => {
                mainWindow.Dispatcher.Invoke((Action)delegate
                {
                    mainWindow.DisconnectClient_Click();
                });
                MessageBox.Show("Клиент TCP: Не удалось получить данные");
            };


            clientUdpSocket.Connect(serverIp, serverPort);
            clientTcpSocket.Connect(serverIp, serverPort);
            isConnect = true;
        }

        public async void ReceiveTcp(UnTcpSocket tcpSocket, IUdpSocket udpSocket)
        {
            while (tcpSocket.isConnect)
            {
                networkReceiveDataTcp.dataDictionary.Clear();
                string ms = tcpSocket.Receive();
                if (ms == null || ms == "") return;
                networkReceiveDataTcp.Unpacking(ms);

                if (networkReceiveDataTcp.dataDictionary.TryGetValue((char)Keys.leftSideScore, out string left))
                {
                    leftSideScore = int.Parse(left);
                }
                if (networkReceiveDataTcp.dataDictionary.TryGetValue((char)Keys.rightSideScore, out string right))
                {
                    rightSideScore = int.Parse(right);
                }
                await Task.Delay(10);
            }
        }
        public async void ReceiveUdp(UnTcpSocket tcpSocket, IUdpSocket udpSocket)
        {
            while (tcpSocket.isConnect)
            {
                networkReceiveDataUdp.dataDictionary.Clear();

                string ms = udpSocket.Receive();
                if (ms == null || ms == "") return;
                networkReceiveDataUdp.Unpacking(ms);

                if (networkReceiveDataUdp.dataDictionary.TryGetValue((char)Keys.paddlePosY, out string y))
                {
                    paddleOpponent.position = new Vector2(paddleOpponent.position.x, int.Parse(y));
                }

                for (int i = 0; i < balls.Count; i++)
                {
                    if (networkReceiveDataUdp.dataDictionary.TryGetValue((char)(i - 1), out string vec))
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
                await Task.Delay(10);
            }
        }

        public async void SendTcp(UnTcpSocket tcpSocket, IUdpSocket udpSocket)
        {
            while (tcpSocket.isConnect)
            {
                tcpSocket.Send(networkSendDataTcp.ToString());

                networkSendDataTcp.dataDictionary.Clear();
                await Task.Delay(200);
            }
        }

        public async void SendUdp(UnTcpSocket tcpSocket, IUdpSocket udpSocket)
        {
            while (tcpSocket.isConnect)
            {
                networkSendDataUdp.dataDictionary.Add((char)Keys.paddlePosY, ((int)paddleLocal.position.y).ToString());

                udpSocket.Send(networkSendDataUdp.ToString());

                networkSendDataUdp.dataDictionary.Clear();
                await Task.Delay(20);
            }
        }

        public void Disconnect()
        {
            isConnect = false;
            clientTcpSocket.Disconnect();
            clientUdpSocket.Disconnect();
        }

        // GAME LOOOOOOOOOOOP
        public void GameSetUp()
        {
            rectLocal.Visibility = Visibility.Visible;
            rectOpponent.Visibility = Visibility.Visible;
            int xPos = 0;
            xPos = (int)canvas.Width - (int)rectOpponent.Width;
            paddleLocal = new Paddle(new Vector2(1, canvas.Height / 2), rectLocal, canvas);
            paddleOpponent = new Paddle(new Vector2(xPos - 1, canvas.Height / 2), rectOpponent, canvas);
           
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
            mainWindow.leftSideScore.Content = leftSideScore.ToString();
            mainWindow.rightSideScore.Content = rightSideScore.ToString();

            foreach (Ball ball in balls)
            {
                ball.UpdateVisibility();
                if (!ball.visible) continue;
                ball.UpdatePos();
            }
            
        }

        public void ResetGame()
        {
            leftSideScore = 0;
            rightSideScore = 0;
            mainWindow.leftSideScore.Content = leftSideScore.ToString();
            mainWindow.rightSideScore.Content = rightSideScore.ToString();

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
