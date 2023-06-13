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

        StringBuilder data = new StringBuilder();

        public virtual string Receive()
        {
            if (socketListner == null) return null;
            socketListner.ReceiveBufferSize = 1;
            try
            {
                byte[] buffer = new byte[1];
                int countBytes = 0;
                data.Clear();
                int available = socketListner.Available;
                do
                {
                    countBytes = socketListner.Receive(buffer);
                    data.Append(Encoding.UTF8.GetString(buffer, 0, countBytes));
                    Trace.WriteLine(Encoding.UTF8.GetString(buffer, 0, countBytes));
                }
                while (socketListner.Available > 0);
                return data.ToString();
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

        public virtual void Send(string message)
        {
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
            isConnect = false;
            isActive = false;
            socketListner?.Shutdown(SocketShutdown.Both);
            socketListner?.Close();
            tcpSocet.Close();
        }
    }
}