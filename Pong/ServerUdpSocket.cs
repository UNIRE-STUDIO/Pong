using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pong
{
    class ServerUdpSocket : IUdpSocket
    {
        protected IPEndPoint udpEndPoint;
        protected Socket udpSocket;

        private EndPoint clientEndPoint;

        public bool isActive = false;

        public EventHandler eventStart;
        public EventHandler eventConnect;
        public EventHandler eventErrorSend;
        public EventHandler eventErrorReceive;

        StringBuilder receiveData = new StringBuilder();

        private byte[] buffer = new byte[128];
        private EndPoint sender;

        public void Start(int port)
        {
            udpEndPoint = new IPEndPoint(IPAddress.Any, port);
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(udpEndPoint);

            // Обязательно перед ref при приеме EndPoint должен быть инициализирован
            clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            isActive = true;
            eventStart?.Invoke(this, null);
        }

        public virtual string Receive(out int errorId)
        {
            try
            {
                udpSocket.ReceiveBufferSize = 8;
                int countBytes = 0;
                receiveData.Clear();
                do
                {
                    countBytes = udpSocket.ReceiveFrom(buffer, ref clientEndPoint);
                    receiveData.Append(Encoding.UTF8.GetString(buffer, 0, countBytes));
                }
                while (udpSocket.Available > 0);
            }
            catch (Exception e)
            {
                eventErrorReceive?.Invoke(this, null);
            }
            if (receiveData.ToString() == "") // Клиент закрыл соединение, мы получили пустую строку
            {
                eventErrorReceive?.Invoke(this, null);
                errorId = 1;
                return null;
            }
            errorId = 0;
            return receiveData.ToString();
        }

        public virtual void Send(string message)
        {
            udpSocket.SendTo(Encoding.UTF8.GetBytes(message), clientEndPoint);
        }

        public void Disconnect()
        {
            isActive = false;
            udpSocket?.Close();
        }
    }
}
