using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pong
{
    interface IUdpSocket
    {
        string Receive(out int errorId);

        void Send(string message);

        void Disconnect();
    }
}
