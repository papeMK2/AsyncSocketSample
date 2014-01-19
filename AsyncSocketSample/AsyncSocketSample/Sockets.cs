using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncSocketSample
{
    class Sockets
    {
        //接続用のソケット
        public Socket client { get; set; }
        //接続するサーバ名
        public string serverName { get; set; }
        //接続するポート番号
        public int portNum { get; set; }

        public Sockets(string ServerName, int PortNum)
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverName = ServerName;
            portNum = PortNum;
        }
    }
}
