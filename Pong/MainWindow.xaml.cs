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
        GameManager gameManager;
        InputManager inputManager;
        SettingsManager settingsManager;

        ServerTcpSocket serverSocket;
        ClientTcpSocket clientSocket;

        public MainWindow()
        {
            InitializeComponent();
            inputManager = new InputManager();
            KeyDown += new KeyEventHandler(inputManager.OnKeyDown);
            KeyUp += new KeyEventHandler(inputManager.OnKeyUp);
            gameManager = new GameManager(this, inputManager);
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
            serverSocket = new ServerTcpSocket();
            serverSocket.eventStart += (hendler, ee) => {
                gameManager.isHost = true;
                gameManager.GameSetUp();
                FpsUpdate();
                serverStatus.Content = "Сервер запущен, ожидаем подключения...";
            };
            serverSocket.eventErrorStart += (hendler, ee) =>
            {
                MessageBox.Show("Сервер: Не удалось запустить сервер");
            };
            serverSocket.eventConnect += async (hendler, ee) => {
                serverStatus.Content = "Сервер: Подключение установлено!";
                await Task.Run(() => gameManager.SendPos(serverSocket));
                int errorId = await Task.Run(() => gameManager.ReceivePos(serverSocket));
                if (errorId == 1) DisconnectServer_Click();
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

            clientSocket = new ClientTcpSocket();
            clientSocket.eventStart += (handle, ee) =>
            {
                clentStatus.Content = "Подключение...";
            };
            clientSocket.eventConnect += async (handle, ee) =>
            {
                gameManager.isHost = false;
                clentStatus.Content = "Клиент: Подключение установлено";
                gameManager.GameSetUp();
                await Task.Run(() => gameManager.SendPos(clientSocket));
                int errorId = await Task.Run(() => gameManager.ReceivePos(clientSocket));
                if (errorId == 2) DisconnectClient_Click();
            };
            clientSocket.eventErrorConnect += (ee, args) =>
            {
                // Вывести через делегаты
                MessageBox.Show("Клиент: Не удалось подключиться... " + ((Exception)ee).Message);
                DisconnectClient_Click();
            };
            
            clientSocket.Connect(serverIp, serverPort);

            clientMenu.Visibility = Visibility.Visible;
            joinMenu.Visibility = Visibility.Hidden;
        }

        public void DisconnectClient_Click(object sender = null, RoutedEventArgs e = null)
        {
            gameManager.StopGameLoop();
            gameManager.ResetGame();
            rectLocal.Visibility = Visibility.Hidden;
            rectOpponent.Visibility = Visibility.Hidden;

            clientMenu.Visibility = Visibility.Hidden;
            clientSocket.Disconnect();
            joinMenu.Visibility = Visibility.Visible;
        }

        public void DisconnectServer_Click(object sender = null, RoutedEventArgs e = null)
        {
            gameManager.StopGameLoop();
            gameManager.ResetGame();
            rectLocal.Visibility = Visibility.Hidden;
            rectOpponent.Visibility = Visibility.Hidden;

            serverMenu.Visibility = Visibility.Hidden;
            serverSocket.Disconnect();
            createMenu.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            gameManager.StopGameLoop();
            gameManager.ResetGame();
            if (serverSocket != null)
            {
                serverSocket.Disconnect();
            }
            else if (clientSocket != null)
            {
                clientSocket.Disconnect();
            }
        }

        private async void FpsUpdate()
        {
            while (gameManager.isActiveGameLoop)
            {
                labelFps.Content = gameManager.fps.ToString();
                await Task.Delay(300);
            }
        }

        private void FirstSize_Click(object sender, RoutedEventArgs e) => settingsManager.ChangeSizeWindow(0);
        private void SecondSize_Click(object sender, RoutedEventArgs e) => settingsManager.ChangeSizeWindow(1);
    }
}
