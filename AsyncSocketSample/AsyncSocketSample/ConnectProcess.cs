using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncSocketSample
{
    class ConnectProcess
    {
        //MainWindowにアクセスするためのポインタ
        public MainWindow MainWindowPointer;

        //スレッド管理用の変数
        private ManualResetEvent connectDone;

        private ManualResetEvent sendDone;

        private ManualResetEvent receiveDone;

        /// <summary>
        /// コンストラクタ
        /// 接続, 送信, 受信のスレッド管理用クラスのインスタンス生成とか
        /// </summary>
        /// <param name="pointer">MainWindowのポインタ</param>
        public ConnectProcess(MainWindow pointer)
        {
            MainWindowPointer = pointer;
            connectDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            receiveDone = new ManualResetEvent(false);
        }

        /// <summary>
        /// メッセージの送受信の開始
        /// </summary>
        /// <param name="socket">接続するソケット情報を格納したクラス</param>
        public void StartClient(Sockets socket)
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(socket.serverName);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, socket.portNum);

                Connect(remoteEP, socket.client);
                connectDone.WaitOne();

                string reqMessage = string.Format("GET / HTTP/1.1\r\n Host: {0} \r\n Connection: Close\r\n\r\n", socket.serverName);

                Send(socket.client, reqMessage);
                sendDone.WaitOne();

                Receive(socket.client);
                receiveDone.WaitOne();

                socket.client.Shutdown(SocketShutdown.Both);
                socket.client.Close();
            }
            catch (Exception)
            {
            }
        }

        #region コネクション処理

        /// <summary>
        /// 接続確立時に呼び出すメソッド
        /// 指定したエンドポイントに対して接続を試みる
        /// </summary>
        /// <param name="remoteEP">設定したエンドポイント</param>
        /// <param name="client">接続に使用するソケット</param>
        public void Connect(EndPoint remoteEP, Socket client)
        {
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();
        }

        /// <summary>
        /// 接続時に呼び出されるコールバックメソッド
        /// </summary>
        /// <param name="result"></param>
        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                Socket client = (Socket)result.AsyncState;
                client.EndConnect(result);
                connectDone.Set();
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region メッセージの送信処理

        /// <summary>
        /// メッセージ送信時に呼び出すメソッド
        /// UTF-8でメッセージをエンコードして送信する
        /// </summary>
        /// <param name="client">送信に使用するソケット</param>
        /// <param name="msg">送信するメッセージ</param>
        public void Send(Socket client, string msg)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(msg);

            client.BeginSend(byteData,
                0,
                byteData.Length,
                SocketFlags.None,
                new AsyncCallback(SendCallback),
                client);
        }

        /// <summary>
        /// メッセージ送信時のコールバックメソッド
        /// </summary>
        /// <param name="result"></param>
        private void SendCallback(IAsyncResult result)
        {
            try
            {
                Socket client = (Socket)result.AsyncState;
                int bytesSent = client.EndSend(result);
                sendDone.Set();
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region 受信処理

        /// <summary>
        /// メッセージを受信するために呼び出すメソッド
        /// </summary>
        /// <param name="client">受信に使用するソケット</param>
        public void Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = client;
                client.BeginReceive(state.buffer,
                    0,
                    StateObject.BufferSize,
                    0,
                    new AsyncCallback(ReceiveCallback),
                    state);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 受信時に呼び出されるコールバックメソッド
        /// メッセージを受信してMainWindowのtexboxに挿入
        /// </summary>
        /// <param name="result"></param>
        private void ReceiveCallback(IAsyncResult result)
        {
            StateObject state = (StateObject)result.AsyncState;
            Socket client = state.workSocket;
            int bytesRead = client.EndReceive(result);
            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                client.BeginReceive(state.buffer,
                    0,
                    StateObject.BufferSize,
                    0,
                    new AsyncCallback(ReceiveCallback),
                    state);
            }
            else
            {
                OutputMessage(state);

                receiveDone.Set();
            }
        }

        //メッセージをtexboxに挿入
        //コントローラは別スレッドなのでDispatcher.BeginInvokeでアクションを設定する
        private void OutputMessage(StateObject state)
        {
            if (state.sb.Length > 1)
            {
                MainWindowPointer.ResultTextBox.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            MainWindowPointer.ResultTextBox.Text = state.sb.ToString();
                        })
                        );
            }
        }
        #endregion
    }
}
