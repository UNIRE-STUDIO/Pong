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

        public virtual string Receive()
        {
            if (socketListner == null)
            {
                eventErrorReceive?.Invoke(this, null);
                return null;
            }
            socketListner.ReceiveBufferSize = 8;
            try
            {
                byte[] buffer = new byte[8];
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
                eventErrorReceive?.Invoke(this, null);
                return null;
            }
        }

        public virtual void Send(string message)
        {
            if (socketListner == null)
            {
                eventErrorReceive?.Invoke(this, null);
                return;
            }
            try
            {
                socketListner.Send(Encoding.UTF8.GetBytes(message));
            }
            catch (Exception e)
            {
                eventErrorSend?.Invoke(this, null);
            }
        }

        public virtual void Disconnect()
        {
            
        }
    }
}