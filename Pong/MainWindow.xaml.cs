using System;
using System.Collections.Generic;
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

        ServerTcpSocket serverSocket;
        ClientTcpSocket clientSocket;

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
                string data = socket.Receive();
                if (!int.TryParse(data, out int y)) return;
                paddleOpponent.SetPos(new Vector2(0, y));
                await Task.Delay(30);
            }
        }

        private async void SendPos(UnTcpSocket socket)
        {
            while (socket.isConnect)
            {
                string message = paddleLocal.position.y.ToString();
                socket.Send(message);

                await Task.Delay(70);
            }
        }

        // GAME LOOOOOOOOOOOP
        private void GameSetUp()
        {
            rectLocal.Visibility = Visibility.Visible;
            rectOpponent.Visibility = Visibility.Visible;
            int xPos = 0;
            if (serverSocket != null) 
            {
                xPos = (int)canvas.Width - (int)rectLocal.Width;
                paddleLocal = new Paddle(new Vector2(xPos, 0), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(0, 0), rectOpponent, canvas);
            }
            else
            {
                xPos = (int)canvas.Width - (int)rectOpponent.Width;
                paddleLocal = new Paddle(new Vector2(0, 0), rectLocal, canvas);
                paddleOpponent = new Paddle(new Vector2(xPos, 0), rectOpponent, canvas);
            }
            GamerLoop();
        }

        private async void GamerLoop()
        {
            // Set gameloop state
            Running = true;

            while (Running)
            {
                int direction = 0;
                if ((Keyboard.GetKeyStates(Key.Down) & KeyStates.Down) > 0)
                {
                    direction = 1;
                }
                else if ((Keyboard.GetKeyStates(Key.Up) & KeyStates.Down) > 0)
                {
                    direction = -1;
                }
                paddleLocal.Direction = direction;
                paddleLocal.UpdateDirection();
                paddleOpponent.UpdatePos();
                await Task.Delay(16);
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
            serverMenu.Visibility = Visibility.Hidden;
            serverSocket.Disconnect();
            serverMenu.Visibility = Visibility.Visible;
        }
    }
}
