using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading;

namespace Pong
{
    class UnTcpSocket
    {
        protected IPEndPoint tcpEndPoint;
        protected Socket tcpSocet;

        public bool isActive = false;
        public bool isConnect = false;
        protected Socket socketListner;

        public EventHandler eventStart;
        public EventHandler eventConnect;
        public EventHandler eventErrorSend;
        public EventHandler eventErrorReceive;

        public int sendingDelay = 70;
        public int receivingDelay = 30;

        StringBuilder receiveData = new StringBuilder();
        StringBuilder sendData = new StringBuilder();

        NetworkData networkSendData = new NetworkData();
        protected CancellationTokenSource cts = new CancellationTokenSource();


        public virtual string Receive()
        {
            if (socketListner == null) return null;
            socketListner.ReceiveBufferSize = 1;
            try
            {
                byte[] buffer = new byte[4];
                int countBytes = 0;
                receiveData.Clear();
                do
                {
                    countBytes = socketListner.Receive(buffer);
                    receiveData.Append(Encoding.UTF8.GetString(buffer, 0, countBytes));
                }
                while (socketListner.Available > 0);
                return receiveData.ToString();
            }
            catch (Exception)
            {
                isConnect = false;
                isActive = false;
                eventErrorReceive?.Invoke(this, null);
                Disconnect();
                return null;
            }
        }

        public virtual async void Send(string message)
        {
            //while (isActive && isConnect)
            if (socketListner == null) return;
            try
            {
                socketListner.Send(Encoding.UTF8.GetBytes(message));
            }
            catch (Exception e)
            {
                isConnect = false;
                isActive = false;
                eventErrorSend?.Invoke(this, null);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (isConnect)
            {
                socketListner.Shutdown(SocketShutdown.Both);
            }
            isConnect = false;
            isActive = false;
            tcpSocet?.Close();
        }
    }
}