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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pong
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Paddle paddleLocal;
        Paddle paddleOpponent;
        Ball ball;

        ServerTcpSocket serverSocket;
        ClientTcpSocket clientSocket;

        NetworkData networkSendData = new NetworkData();
        NetworkData networkReceiveData = new NetworkData();

        public bool Running { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            startMenu.Visibility = Visibility.Hidden;
            createMenu.Visibility = Visibility.Visible;
        }

        private void ButtonJoin_Click(object sender, RoutedEventArgs e)
        {
            startMenu.Visibility = Visibility.Hidden;
            joinMenu.Visibility = Visibility.Visible;
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            startMenu.Visibility = Visibility.Visible;
            createMenu.Visibility = Visibility.Hidden;
            joinMenu.Visibility = Visibility.Hidden;
        }

        // Серверная часть
        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            int port;
            try
            {
                port = int.Parse(textBoxPortCreate.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Неверный формат");
                return;
            }
            serverSocket = new ServerTcpSocket();
            serverSocket.eventStart += (hendler, ee) => {
                GameSetUp();
                serverStatus.Content = "Сервер запущен, ожидаем подключения...";
            };
            serverSocket.eventErrorStart += (hendler, ee) =>
            {
                MessageBox.Show("Сервер: Не удалось запустить сервер");
            };
            serverSocket.eventConnect += async (hendler, ee) => {
                serverStatus.Content = "Сервер: Подключение установлено!";
                await Task.Run(() => SendPos(serverSocket));
                await Task.Run(() => ReceivePos(serverSocket));
            };
            serverSocket.eventErrorReceive += (hendler, ee) => {
                MessageBox.Show("Сервер: Не удалось получить данные");
            };
            serverSocket.eventErrorSend += (hendler, ee) => {
                MessageBox.Show("Сервер: Не удалось отправить данные");
            };

            serverSocket.Start(port);

            createMenu.Visibility = Visibility.Hidden;
            serverMenu.Visibility = Visibility.Visible;
            //serverStatus.Content = $"Адрес подключенного клиента: {client.RemoteEndPoint}";
        }

        // Клиентская часть
        private void ConnectRoom_Click(object sender, RoutedEventArgs e)
        {
            string serverIp;
            int serverPort;
            try
            {
                serverIp = textBoxIp.Text;
                serverPort = int.Parse(textBoxPort.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Неверный формат");
                return;
            }
            clientSocket = new ClientTcpSocket();
            clientSocket.eventStart += (handle, ee) =>
            {
                clentStatus.Content = "Подключение...";
            };
            clientSocket.eventConnect += async (handle, ee) =>
            {
                GameSetUp();
                await Task.Run(() => SendPos(clientSocket));
                await Task.Run(() => ReceivePos(clientSocket));
                clentStatus.Content = "Подключение установлено";
            };
            clientSocket.eventErrorConnect += (handle, ee) =>
            {
                MessageBox.Show("Клиент: Не удалось подключиться...");
            };
            
            clientSocket.Connect(serverIp, serverPort);

            clientMenu.Visibility = Visibility.Visible;
            joinMenu.Visibility = Visibility.Hidden;
        }

        private async void ReceivePos(UnTcpSocket socket)
        {
            while (socket.isConnect)
            {
                networkReceiveData.dataDictionary.Clear();
                networkReceiveData.Unpacking(socket.Receive());

                if (networkReceiveData.dataDictionary.TryGetValue((char)112, out int y))
                {
                    paddleOpponent.position = new Vector2(paddleOpponent.position.x, y);
                }
                
                if (clientSocket != null)
                { 
                    if (networkReceiveData.dataDictionary.TryGetValue((char)113, out int posX) &&
                        networkReceiveData.dataDictionary.TryGetValue((char)114, out int posY))
                        ball.position = new Vector2(posX, posY);
                }
                await Task.Delay(30);
            }
        }

        private async void SendPos(UnTcpSocket socket)
        {
            while (socket.isConnect)
            {
                networkSendData.dataDictionary.Clear();
                networkSendData.dataDictionary.Add((char)112, paddleLocal.position.y);
                if (serverSocket != null)
                {
                    networkSendData.dataDictionary.Add((char)113, ball.position.x);
                    networkSendData.dataDictionary.Add((char)114, ball.position.y);
                }
                socket.Send(networkSendData.ToString());

                await Task.Delay(50);
            }
        }

        private void GameExit()
        {
            isActiveGameLoop = false;
            rectLocal.Visibility = Visibility.Hidden;
            rectOpponent.Visibility = Visibility.Hidden;
            EllipseBall.Visibility = Visibility.Hidden;
        }

        // GAME LOOOOOOOOOOOP
        private void GameSetUp()
        {
            rectLocal.Visibility = Visibility.Visible;
            rectOpponent.Visibility = Visibility.Visible;
            EllipseBall.Visibility = Visibility.Visible;
            int xPos = 0;
            if (serverSocket != null) 
            {
                xPos = (int)canvas.Width - (int)rectLocal.Width;
                paddleLocal = new Paddle(new Vector2(xPos, 0), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(0, 0), rectOpponent, canvas);
                ball = new Ball(new Vector2((int)canvas.Width/2, 50), EllipseBall, canvas);
            }
            else
            {
                xPos = (int)canvas.Width - (int)rectOpponent.Width;
                paddleLocal = new Paddle(new Vector2(0, 0), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(xPos, 0), rectOpponent, canvas);
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
                if (keyDownIsPressed)
                {
                    direction = 1;
                }
                else if (keyUpIsPressed)
                {
                    direction = -1;
                }
                paddleLocal.Direction = new Vector2(paddleLocal.Direction.x, direction);
                paddleLocal.Calculation();
                paddleLocal.UpdatePos();
                paddleOpponent.UpdatePos();

                // Поменять на BOOL IsServer
                if (serverSocket != null && !isPause)
                {
                    ball.position.x = ball.position.x + ball.Direction.x * ball.Speed;
                    ball.position.y = ball.position.y + ball.Direction.y * ball.Speed;

                    // Касаемся ракеток
                    if (ball.position.x < rectLocal.Width && 
                        ball.position.y > paddleOpponent.position.y - EllipseBall.Width && 
                        ball.position.y < paddleOpponent.position.y + rectOpponent.Height)
                    {
                        ball.Direction.x = -ball.Direction.x;
                    }
                    else if (ball.position.x > canvas.Width - rectLocal.Width - EllipseBall.Width &&
                             ball.position.y > paddleLocal.position.y - EllipseBall.Width &&
                             ball.position.y < paddleLocal.position.y + rectLocal.Height)
                    {
                        ball.Direction.x = -ball.Direction.x;
                    }

                    if (ball.position.y >= canvas.Height-EllipseBall.Width || 
                        ball.position.y <= 0)
                    {
                        ball.Direction.y = -ball.Direction.y;
                    }

                    if (ball.position.x <= 0)
                    {
                        rightSideScore.Content = int.Parse(rightSideScore.Content.ToString()) + 1;
                        isPause = true;
                        await Task.Delay(1000);
                        Random rand = new Random();
                        ball.position = new Vector2((int)canvas.Width/2, rand.Next(0, (int)(canvas.Height-EllipseBall.Height)));
                    }
                    else if (ball.position.x > canvas.Width)
                    {
                        leftSideScore.Content = int.Parse(leftSideScore.Content.ToString()) + 1;
                        isPause = true;
                        await Task.Delay(1000);
                        Random rand = new Random();
                        ball.position = new Vector2((int)canvas.Width / 2, rand.Next(0, (int)(canvas.Height - EllipseBall.Height)));
                    }
                }
                ball.UpdatePos();
                await Task.Delay(16);
            }
        }

        bool keyUpIsPressed = false;
        bool keyDownIsPressed = false;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Up)
            {
                keyUpIsPressed = true;
            }

            if (e.Key == Key.Down)
            {
                keyDownIsPressed = true;
            }

            if (e.Key == Key.Space)
            {
                isPause = !isPause;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key == Key.Up)
            {
                keyUpIsPressed = false;
            }

            if (e.Key == Key.Down)
            {
                keyDownIsPressed = false;
            }
        }


        private void DisconnectClient_Click(object sender, RoutedEventArgs e)
        {
            clientMenu.Visibility = Visibility.Hidden;
            clientSocket.Disconnect();
            joinMenu.Visibility = Visibility.Visible;
        }

        private void DisconnectServer_Click(object sender, RoutedEventArgs e)
        {
            GameExit();
            serverMenu.Visibility = Visibility.Hidden;
            serverSocket.Disconnect();
            createMenu.Visibility = Visibility.Visible;
        }
    }
}
