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
    class ServerTcpSocket : UnTcpSocket
    {


        public EventHandler eventErrorStart;
        public async void Start(int port)
        {
            try
            {
                tcpEndPoint = new IPEndPoint(IPAddress.Any, port);
                tcpSocet = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpSocet.Bind(tcpEndPoint); // Связывание
                tcpSocet.Listen(1);
                isActive = true;
                eventStart?.Invoke(this, null);
                socketListner = await Task.Run(() => tcpSocet.Accept());
                isConnect = true;
                eventConnect?.Invoke(this, null);
            }
            catch (Exception)
            {
                isActive = false;
                isConnect = false;
                eventErrorStart?.Invoke(this, null);
            }
        }
    }
}
