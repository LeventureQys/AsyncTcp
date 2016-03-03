using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    internal class OutgoingDataPreparer
    {
        internal void PrepareOutgoingData(SocketAsyncEventArgs sendEventArgs, byte[] sendData)
        {
            DataHoldingUserToken sendToken = (DataHoldingUserToken)sendEventArgs.UserToken;

            //要发送的数据长度
            int lengthOfMessage = sendData.Length;

            //头长度的字节数。
            byte[] arrayOfBytesInPrefix = BitConverter.GetBytes(lengthOfMessage);

            sendToken.DataToSend = new byte[sendToken.PrefixLength + lengthOfMessage];

            Buffer.BlockCopy(arrayOfBytesInPrefix, 0, sendToken.DataToSend, 0, sendToken.PrefixLength);
            Buffer.BlockCopy(sendData, 0, sendToken.DataToSend, sendToken.PrefixLength, lengthOfMessage);
            sendToken.LengthOfMessage = sendToken.DataToSend.Length;
            sendToken.MessageBytesDoneCount = 0;
        }
    }
}
