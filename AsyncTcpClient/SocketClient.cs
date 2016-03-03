using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    public class SocketClient
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
        /// 连接对象
        /// </summary>
        private SocketAsyncEventArgs connectSaea;

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

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="socketSetting"></param>
        public SocketClient(SocketSetting socketSetting)
        {
            this.socketSetting = socketSetting;
            this.prefixHandler = new PrefixHandler();
            this.messageHandler = new MessageHandler();
            this.outgoingDataPreparer=new OutgoingDataPreparer();

            this.buffer = new BufferManager(
                socketSetting.BufferSize * socketSetting.NumberOfSaeaForRec * socketSetting.NumberOfSaeaForSend,
                socketSetting.BufferSize);

            this.connectSaea =new SocketAsyncEventArgs();
            this.connectSaea.Completed += IO_Completed;

            this.poolOfReceiveSaea = new SocketAsyncEventArgsPool(socketSetting.NumberOfSaeaForRec);
            this.poolOfSendSaea = new SocketAsyncEventArgsPool(socketSetting.NumberOfSaeaForSend);

            Init();

        }

        private void Init()
        {
            this.buffer.InitBuffer();

          
            for (int i = 0; i < socketSetting.NumberOfSaeaForRec; i++)
            {
                this.poolOfReceiveSaea.Push(CreateNewSaeaForRecSen());
            }

            for (int i = 0; i < socketSetting.NumberOfSaeaForSend; i++)
            {
                this.poolOfSendSaea.Push(CreateNewSaeaForRecSen());
            }
        }

        public void StartConnect()
        {
            connectSaea.RemoteEndPoint = this.socketSetting.LocalEndPoint;
            connectSaea.AcceptSocket=new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            bool willRaiseEvent = connectSaea.AcceptSocket.ConnectAsync(connectSaea);
            if (!willRaiseEvent)
            {
                ProcessConnect(connectSaea);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void ProcessConnect(SocketAsyncEventArgs e)
        {

            if (e.SocketError != SocketError.Success)
            {
                ProcessConnectionError(e);
            }
            else
            {
                SocketAsyncEventArgs receiveEventArgs = this.poolOfReceiveSaea.Pop();
                receiveEventArgs.AcceptSocket = e.AcceptSocket;
                StartReceive(receiveEventArgs);
                if (ConnectedEvent != null)
                {
                    ConnectedEvent(e.AcceptSocket);
                }
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
                StartDisConnect(receiveEventArgs, true);
                return;
            }

            if (receiveEventArgs.BytesTransferred == 0)
            {
                receiveSendToken.Reset();
                StartDisConnect(receiveEventArgs, true);
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
                StartDisConnect(sendEventArgs, false);
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


        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    ProcessDisConnectAndCloseSocket(e,true);
                    break;
                default:
                {
                    throw new ArgumentException("错误 I/O 操作");
                }

            }
        }


        private void ProcessConnectionError(SocketAsyncEventArgs connectEventArgs)
        {
            if ((connectEventArgs.SocketError != SocketError.ConnectionRefused) && (connectEventArgs.SocketError != SocketError.TimedOut) && (connectEventArgs.SocketError != SocketError.HostUnreachable))
            {
                CloseSocket(connectEventArgs.AcceptSocket);
            }           

        }

        private void CloseSocket(Socket theSocket)
        {
            try
            {
                theSocket.Shutdown(SocketShutdown.Both);
            }
            catch 
            {
               
            }
            theSocket.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receiveSendEventArgs"></param>
        /// <param name="isRec"></param>
        private void StartDisConnect(SocketAsyncEventArgs receiveSendEventArgs,bool isRec)
        {
            receiveSendEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            bool willRaiseEvent = receiveSendEventArgs.AcceptSocket.DisconnectAsync(receiveSendEventArgs);
            if (!willRaiseEvent)
            {
                ProcessDisConnectAndCloseSocket(receiveSendEventArgs, isRec);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receiveSendEventArgs"></param>
        /// <param name="isRec"></param>
        private void ProcessDisConnectAndCloseSocket(SocketAsyncEventArgs receiveSendEventArgs,bool isRec)
        {
            receiveSendEventArgs.AcceptSocket.Close();
            if (isRec)
            {
                DataHoldingUserToken userToken = (DataHoldingUserToken) receiveSendEventArgs.UserToken;
                userToken.CreateNewDataHolder();
                this.poolOfReceiveSaea.Push(receiveSendEventArgs);
            }
            else
            {
                this.poolOfSendSaea.Push(receiveSendEventArgs);
            }
        }
    }
}
