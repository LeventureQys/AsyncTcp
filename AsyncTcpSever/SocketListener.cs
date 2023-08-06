using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpSever
{
    public class SocketListener
    {
        #region 变量申明
        /// <summary>
        /// 缓冲池
        /// </summary>
        private BufferManager buffer;


        private Socket listenSocket;

        /// <summary>
        /// 初始信息
        /// </summary>
        private SocketSetting socketSetting;

        /// <summary>
        /// 头处理
        /// </summary>
        private PrefixHandler prefixHandler;

        /// <summary>
        /// 消息接收处理
        /// </summary>
        private MessageHandler messageHandler;

        /// <summary>
        /// 消息发送准备
        /// </summary>
        private OutgoingDataPreparer outgoingDataPreparer;

        /// <summary>
        /// 连接对象池
        /// </summary>
        private SocketAsyncEventArgsPool poolOfAcceptSaea;

        /// <summary>
        /// 发送池
        /// </summary>
        private SocketAsyncEventArgsPool poolOfSendSaea;

        /// <summary>
        /// 接收池
        /// </summary>
        private SocketAsyncEventArgsPool poolOfReceiveSaea;

        public event Action<Socket> ConnectedEvent;

        public event Action<Socket, byte[]> ReceiveEvent;

        public Dictionary<String, Socket> clients;
        /// <summary>
        /// 默认ip和端口号
        /// </summary>
        private String ip = "127.0.0.1";
        private Int16 port = 1145;
        #endregion

        #region 初始化信息
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socketSetting"></param>
        public SocketListener(SocketSetting socketSetting)
        {
            this.socketSetting = socketSetting;
            this.prefixHandler = new PrefixHandler();
            this.messageHandler = new MessageHandler();
            this.outgoingDataPreparer = new OutgoingDataPreparer();

            this.buffer = new BufferManager(
                socketSetting.BufferSize * socketSetting.NumberOfSaeaForRec * socketSetting.NumberOfSaeaForSend,
                socketSetting.BufferSize);
            this.poolOfAcceptSaea = new SocketAsyncEventArgsPool(socketSetting.MaxSimultaneousAcceptOps);
            this.poolOfReceiveSaea = new SocketAsyncEventArgsPool(socketSetting.NumberOfSaeaForRec);
            this.poolOfSendSaea = new SocketAsyncEventArgsPool(socketSetting.NumberOfSaeaForSend);

            Init();

            StartListen();
        }

        /// <summary>
        /// 初始化缓冲及saea对象
        /// </summary>
        private void Init()
        {
            this.buffer.InitBuffer();

            for (int i = 0; i < socketSetting.MaxSimultaneousAcceptOps; i++)
            {
                this.poolOfAcceptSaea.Push(CreateNewSaeaForAccept());
            }

            for (int i = 0; i < socketSetting.NumberOfSaeaForRec; i++)
            {
                this.poolOfReceiveSaea.Push(CreateNewSaeaForRecSen());
            }

            for (int i = 0; i < socketSetting.NumberOfSaeaForSend; i++)
            {
                this.poolOfSendSaea.Push(CreateNewSaeaForRecSen());
            }
        }



        /// <summary>
        /// 开始监听，本地ip和随机端口
        /// </summary>
        private void StartListen()
        {
            listenSocket = new Socket(this.socketSetting.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(this.socketSetting.LocalEndPoint);

            listenSocket.Listen(this.socketSetting.Bakclog);
            StartAccept();
        }
        /// <summary>
        /// 输入ip和端口号，用于监听指定端口
        /// </summary>
        /// <param name="ipAddress">ip</param>
        /// <param name="port">端口</param>
        public void StartListen(string ipAddress, int port)
        {
            IPAddress localIpAddress = IPAddress.Parse(ipAddress);
            IPEndPoint localEndPoint = new IPEndPoint(localIpAddress, port);

            listenSocket = new Socket(localIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);

            listenSocket.Listen(this.socketSetting.Bakclog);
            StartAccept();
        }
        /// <summary>
        /// 创建一个saea对象做接收操作
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateNewSaeaForAccept()
        {
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += AcceptEventArg_Completed;
            return acceptEventArgs;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateNewSaeaForRecSen()
        {
            SocketAsyncEventArgs recSendEventArgs = new SocketAsyncEventArgs();
            this.buffer.SetBuffer(recSendEventArgs);
            recSendEventArgs.Completed += IO_Completed;
            DataHoldingUserToken userToken = new DataHoldingUserToken(recSendEventArgs.Offset, socketSetting.PrefixLength);
            userToken.CreateNewDataHolder();
            recSendEventArgs.UserToken = userToken;
            return recSendEventArgs;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private void StartAccept()
        {
            SocketAsyncEventArgs acceptEventArg;

            if (this.poolOfAcceptSaea.Count > 1)
            {
                try
                {
                    acceptEventArg = this.poolOfAcceptSaea.Pop();
                }
                catch
                {
                    acceptEventArg = CreateNewSaeaForAccept();
                }
            }
            else
            {
                acceptEventArg = CreateNewSaeaForAccept();
            }

            //开始一个异步操作
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);

            //如果是同步完成操作
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        /// <summary>
        /// 做接受操作，并转化为做接收操作
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg.SocketError != SocketError.Success)
            {
                StartAccept();

                HandleBadAccept(acceptEventArg);
                return;
            }

            StartAccept();

            SocketAsyncEventArgs receiveEventArgs = this.poolOfReceiveSaea.Pop();

            receiveEventArgs.AcceptSocket = acceptEventArg.AcceptSocket;

            acceptEventArg.AcceptSocket = null;

            this.poolOfAcceptSaea.Push(acceptEventArg);

            StartReceive(receiveEventArgs);
            if (ConnectedEvent != null)
            {
                ConnectedEvent(receiveEventArgs.AcceptSocket);
                string ipAddress = ((IPEndPoint)acceptEventArg.RemoteEndPoint).Address.ToString();
                this.clients.Add(ipAddress, receiveEventArgs.AcceptSocket);
            }
        }

        /// <summary>
        /// 开始接收操作
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        private void StartReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            DataHoldingUserToken receiveToken = (DataHoldingUserToken)receiveEventArgs.UserToken;

            receiveEventArgs.SetBuffer(receiveToken.BufferOffset, socketSetting.BufferSize);

            bool willRaiseEvent = receiveEventArgs.AcceptSocket.ReceiveAsync(receiveEventArgs);

            if (!willRaiseEvent)
            {
                ProcessReceive(receiveEventArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            DataHoldingUserToken receiveSendToken = (DataHoldingUserToken)receiveEventArgs.UserToken;
            if (receiveEventArgs.SocketError != SocketError.Success)
            {
                receiveSendToken.Reset();
                CloseClientSocket(receiveEventArgs, true);
                return;
            }


            if (receiveEventArgs.BytesTransferred > receiveEventArgs.Count)
            {
                receiveSendToken.Reset();

            }
            if (receiveEventArgs.BytesTransferred == 0)
            {
                receiveSendToken.Reset();
                CloseClientSocket(receiveEventArgs, true);
                return;
            }

            int remainingBytesToProcess = receiveEventArgs.BytesTransferred;


            if (receiveSendToken.PrefixByteDoneCount < this.socketSetting.PrefixLength)
            {
                remainingBytesToProcess = prefixHandler.HandlePrefix(receiveEventArgs, receiveSendToken,
                    remainingBytesToProcess);

                if (remainingBytesToProcess == 0)
                {
                    StartReceive(receiveEventArgs);
                    return;
                }
            }

            bool incomingTcpMessageIsReady = messageHandler.HandlerMessage(receiveEventArgs, receiveSendToken,
                remainingBytesToProcess);

            if (incomingTcpMessageIsReady)
            {
                if (ReceiveEvent != null)
                {
                    ReceiveEvent(receiveEventArgs.AcceptSocket, receiveSendToken.Holder.DataMessageReceived);

                }
                receiveSendToken.Holder.DataMessageReceived = null;
                receiveSendToken.Reset();
                StartReceive(receiveEventArgs);

            }
            else
            {
                receiveSendToken.ReceiveMessageOffset = receiveSendToken.BufferOffset;
                receiveSendToken.RecPrefixBytesDoneThisOp = 0;
                StartReceive(receiveEventArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        public void SendData(Socket socket, byte[] data)
        {
            SocketAsyncEventArgs sendEventArgs = this.poolOfSendSaea.Pop();
            if (sendEventArgs == null)
            {
                return;
            }
            sendEventArgs.AcceptSocket = socket;

            outgoingDataPreparer.PrepareOutgoingData(sendEventArgs, data);

            StartSend(sendEventArgs);
        }
        /// <summary>
        /// 向指定客户端发送请求，ip为空则直接广播给所有客户端
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="ip"></param>
        public void SendData(String strMessage, String ip = "")
        {

            if (String.IsNullOrEmpty(strMessage))
            {
                foreach (var socket in this.clients)
                {
                    this.SendData(socket.Value, Encoding.UTF8.GetBytes(strMessage));
                }
            }
            this.SendData(this.clients[ip], Encoding.UTF8.GetBytes(strMessage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendEventArgs"></param>
        private void StartSend(SocketAsyncEventArgs sendEventArgs)
        {
            DataHoldingUserToken sendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
            if (sendToken.LengthOfMessage <= this.socketSetting.BufferSize)
            {
                sendEventArgs.SetBuffer(sendToken.BufferOffset, sendToken.LengthOfMessage);
                Buffer.BlockCopy(sendToken.DataToSend, sendToken.MessageBytesDoneCount, sendEventArgs.Buffer,
                    sendToken.BufferOffset, sendToken.LengthOfMessage);
            }
            else
            {
                sendEventArgs.SetBuffer(sendToken.BufferOffset, this.socketSetting.BufferSize);
                Buffer.BlockCopy(sendToken.DataToSend, sendToken.MessageBytesDoneCount, sendEventArgs.Buffer,
                    sendToken.BufferOffset, this.socketSetting.BufferSize);
            }
            bool willRaiseEvent = sendEventArgs.AcceptSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(sendEventArgs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendEventArgs"></param>
        private void ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            DataHoldingUserToken sendToken = (DataHoldingUserToken)sendEventArgs.UserToken;
            if (sendEventArgs.SocketError != SocketError.Success)
            {
                CloseClientSocket(sendEventArgs, false);
                return;
            }
            sendToken.LengthOfMessage -= sendEventArgs.BytesTransferred;
            if (sendToken.LengthOfMessage == 0)
            {
                sendEventArgs.AcceptSocket = null;
                this.poolOfSendSaea.Push(sendEventArgs);
                return;
            }
            else
            {
                sendToken.MessageBytesDoneCount += sendEventArgs.BytesTransferred;
                StartSend(sendEventArgs);
            }

        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    {
                        ProcessReceive(e);
                        break;
                    }
                case SocketAsyncOperation.Send:
                    {
                        ProcessSend(e);
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("最后一次socket操作不是接收或发送!");
                    }
            }
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        private void HandleBadAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            acceptEventArgs.AcceptSocket.Close();
            poolOfAcceptSaea.Push(acceptEventArgs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isRec"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e, Boolean isRec)
        {
            try
            {
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {

            }
            //用户离开时将其移除
            this.clients.Remove(((IPEndPoint)e.RemoteEndPoint).Address.ToString());
            e.AcceptSocket.Close();

            if (isRec)
            {
                var receiveSendToken = (e.UserToken as DataHoldingUserToken);
                if (receiveSendToken.Holder != null && receiveSendToken.Holder.DataMessageReceived != null)
                {
                    receiveSendToken.CreateNewDataHolder();
                }
                this.poolOfReceiveSaea.Push(e);
            }
            else
            {
                this.poolOfSendSaea.Push(e);
            }
        }
    }
}
