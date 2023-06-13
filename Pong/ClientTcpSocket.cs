using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Windows;

namespace Pong
{
    class ClientTcpSocket : UnTcpSocket
    {
        public EventHandler eventErrorConnect;

        public async void Connect(string ipAddress, int port)
        {
            try
            {
                //EndPoint точка подключение
                tcpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                socketListner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                eventStart?.Invoke(this, null);
                await Task.Run(() => socketListner.Connect(tcpEndPoint));
                isConnect = true;
                eventConnect?.Invoke(this, null);
            }
            catch (Exception)
            {
                // Вывести через делегаты
                MessageBox.Show("Клиент: Не удалось подключиться...");
                eventErrorConnect?.Invoke(this, null);
            }
        }
    }
}
