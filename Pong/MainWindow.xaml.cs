using Microsoft.Win32;
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
        ServerGameManager serverGameManager;
        ClientGameManager clientGameManager;
        InputManager inputManager;
        SettingsManager settingsManager;

        ServerTcpSocket serverTcpSocket;
        ServerUdpSocket serverUdpSocket;

        ClientTcpSocket clientTcpSocket;
        ClientUdpSocket clientUdpSocket;

        public MainWindow()
        {
            InitializeComponent();
            inputManager = new InputManager();
            KeyDown += new KeyEventHandler(inputManager.OnKeyDown);
            KeyUp += new KeyEventHandler(inputManager.OnKeyUp);
            serverGameManager = new ServerGameManager(this, inputManager);
            clientGameManager = new ClientGameManager(this, inputManager);
            settingsManager = new SettingsManager(this);
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.isActive = false;
            startMenu.Visibility = Visibility.Hidden;
            createMenu.Visibility = Visibility.Visible;
        }

        private void ButtonJoin_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.isActive = false;
            startMenu.Visibility = Visibility.Hidden;
            joinMenu.Visibility = Visibility.Visible;
            RegistryKey currentUserKey = Registry.CurrentUser;
            RegistryKey pongKey = currentUserKey.OpenSubKey("pong", true);
            if (pongKey != null && pongKey.GetValue("ip") != null)
            textBoxIp.Text = pongKey.GetValue("ip").ToString();
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.isActive = true;
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
            serverTcpSocket = new ServerTcpSocket();
            serverUdpSocket = new ServerUdpSocket();

            serverTcpSocket.eventStart += (hendler, ee) => {
                serverGameManager.GameSetUp();
                FpsUpdate();
                serverStatus.Content = "Сервер запущен, ожидаем подключения...";
            };

            serverTcpSocket.eventErrorStart += (hendler, ee) =>
            {
                MessageBox.Show("Сервер: Не удалось запустить сервер");
            };
            serverTcpSocket.eventConnect += async (hendler, ee) => {
                serverStatus.Content = "Сервер: Подключение установлено!";

                // У сервера прием должен быть первым, так
                await Task.Run(() => serverGameManager.ReceiveUdp(serverTcpSocket, serverUdpSocket));
                await Task.Run(() => serverGameManager.SendTcp(serverTcpSocket, serverUdpSocket));
                await Task.Run(() => serverGameManager.SendUdp(serverTcpSocket, serverUdpSocket));
            };
            serverUdpSocket.eventErrorReceive += (hendler, ee) => {
                MessageBox.Show("Сервер: Не удалось получить данные");
            };
            serverTcpSocket.eventErrorReceive += (hendler, ee) => {
                MessageBox.Show("Сервер: Не удалось получить данные");
            };
            serverTcpSocket.eventErrorSend += (hendler, ee) => {
                MessageBox.Show("Сервер: Не удалось отправить данные");
            };

            serverTcpSocket.Start(port);
            serverUdpSocket.Start(port);
            
            createMenu.Visibility = Visibility.Hidden;
            serverMenu.Visibility = Visibility.Visible;
            //serverStatus.Content = $"Адрес подключенного клиента: {client.RemoteEndPoint}";
        }

        // Клиентская часть
        private void ConnectRoom_Click(object sender, RoutedEventArgs e)
        {
            settingsManager.isActive = false;
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
            try
            {
                RegistryKey currentUserKey = Registry.CurrentUser;
                if (currentUserKey != null)
                {
                    RegistryKey pongKey = currentUserKey.CreateSubKey("pong");
                    pongKey.SetValue("ip", serverIp);
                    pongKey.Close();
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show("Ошибка сохранения: " + ee.Message);
            }

            clientTcpSocket = new ClientTcpSocket();
            clientUdpSocket = new ClientUdpSocket();
            clientTcpSocket.eventStart += (handle, ee) =>
            {
                clentStatus.Content = "Подключение...";
            };
            clientTcpSocket.eventConnect += async (handle, ee) =>
            {
                clentStatus.Content = "Клиент: Подключение установлено";
                clientGameManager.GameSetUp();
                await Task.Run(() => clientGameManager.SendUdp(clientTcpSocket, clientUdpSocket));
                await Task.Run(() => clientGameManager.ReceiveUdp(clientTcpSocket, clientUdpSocket));
                await Task.Run(() => clientGameManager.ReceiveTcp(clientTcpSocket, clientUdpSocket));
                //if (errorId == 2) DisconnectClient_Click();
            };
            clientTcpSocket.eventErrorConnect += (ee, args) =>
            {
                // Вывести через делегаты
                MessageBox.Show("Клиент: Не удалось подключиться... " + ((Exception)ee).Message);
                DisconnectClient_Click();
            };

            clientUdpSocket.Connect(serverIp, serverPort);
            clientTcpSocket.Connect(serverIp, serverPort);
            

            clientMenu.Visibility = Visibility.Visible;
            joinMenu.Visibility = Visibility.Hidden;
        }

        public void DisconnectClient_Click(object sender = null, RoutedEventArgs e = null)
        {
            clientGameManager.StopGameLoop();
            clientGameManager.ResetGame();
            rectLocal.Visibility = Visibility.Hidden;
            rectOpponent.Visibility = Visibility.Hidden;

            clientMenu.Visibility = Visibility.Hidden;
            clientTcpSocket.Disconnect();
            clientUdpSocket.Disconnect();
            joinMenu.Visibility = Visibility.Visible;
        }

        public void DisconnectServer_Click(object sender = null, RoutedEventArgs e = null)
        {
            serverGameManager.StopGameLoop();
            serverGameManager.ResetGame();
            rectLocal.Visibility = Visibility.Hidden;
            rectOpponent.Visibility = Visibility.Hidden;

            serverMenu.Visibility = Visibility.Hidden;
            serverTcpSocket.Disconnect();
            createMenu.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            serverGameManager.StopGameLoop();
            clientGameManager.StopGameLoop();
            serverGameManager.ResetGame();
            clientGameManager.ResetGame();
            if (serverTcpSocket != null)
            {
                serverTcpSocket.Disconnect();
                serverUdpSocket.Disconnect();
            }
            else if (clientTcpSocket != null)
            {
                clientTcpSocket.Disconnect();
                clientUdpSocket.Disconnect();
            }
        }


        // Убрать внутрь GameManager
        private async void FpsUpdate()
        {
            while (serverGameManager.isActiveGameLoop)
            {
                labelFps.Content = serverGameManager.fps.ToString();
                await Task.Delay(300);
            }
        }

        private void FirstSize_Click(object sender, RoutedEventArgs e) => settingsManager.ChangeSizeWindow(0);
        private void SecondSize_Click(object sender, RoutedEventArgs e) => settingsManager.ChangeSizeWindow(1);
    }
}
