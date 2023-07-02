using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pong
{
    class ClientUdpSocket : IUdpSocket
    {
        protected IPEndPoint udpEndPoint;
        protected Socket udpSocket;

        private EndPoint serverEndPoint;

        public bool isActive = false;
        public bool isConnect = false;

        public EventHandler eventStart;
        public EventHandler eventConnect;
        public EventHandler eventErrorSend;
        public EventHandler eventErrorReceive;

        StringBuilder receiveData = new StringBuilder();

        byte[] buffer = new byte[128];

        public void Connect(string ipAddress, int port)
        {
            udpEndPoint = new IPEndPoint(IPAddress.Any, 49400);
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(udpEndPoint);

            serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            eventStart?.Invoke(this, null);
        }
        public virtual string Receive()
        {
            try
            {
                int countBytes = 0;
                receiveData.Clear();
                do
                {
                    countBytes = udpSocket.ReceiveFrom(buffer, ref serverEndPoint);
                    receiveData.Append(Encoding.UTF8.GetString(buffer, 0, countBytes));
                }
                while (udpSocket.Available > 0);
            }
            catch (Exception e)
            {
                eventErrorReceive?.Invoke(this, null);
                return null;
            }
            if (receiveData.ToString() == "") // Клиент закрыл соединение, мы получили пустую строку
            {
                eventErrorReceive?.Invoke(this, null);
                return null;
            }
            return receiveData.ToString();
        }

        public virtual void Send(string message)
        {
            udpSocket.SendTo(Encoding.UTF8.GetBytes(message), serverEndPoint);
        }

        public void Disconnect()
        {
            isConnect = false;
            isActive = false;
            udpSocket?.Close();
        }
    }
}
