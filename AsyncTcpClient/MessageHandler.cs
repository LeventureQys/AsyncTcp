using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    internal class MessageHandler
    {
        internal bool HandlerMessage(SocketAsyncEventArgs receiveEventArgs, DataHoldingUserToken receiveToken,
            int remainingBytesToProcess)
        {
            bool incomingTcpMessageIsReady = false;

            if (receiveToken.MessageBytesDoneCount == 0)
            {
                receiveToken.Holder.DataMessageReceived = new byte[receiveToken.LengthOfMessage];
            }

            if (remainingBytesToProcess + receiveToken.MessageBytesDoneCount == receiveToken.LengthOfMessage)
            {
                Buffer.BlockCopy(receiveEventArgs.Buffer, receiveToken.ReceiveMessageOffset,
                    receiveToken.Holder.DataMessageReceived, receiveToken.MessageBytesDoneCount, remainingBytesToProcess);
                incomingTcpMessageIsReady = true;
            }
            else
            {
                Buffer.BlockCopy(receiveEventArgs.Buffer, receiveToken.ReceiveMessageOffset,
                    receiveToken.Holder.DataMessageReceived, receiveToken.MessageBytesDoneCount, remainingBytesToProcess);
                receiveToken.ReceiveMessageOffset = receiveToken.ReceiveMessageOffset -
                                                    receiveToken.RecPrefixBytesDoneThisOp;
                receiveToken.MessageBytesDoneCount += remainingBytesToProcess;
            }
            return incomingTcpMessageIsReady;
        }
    }
}
