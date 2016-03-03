using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpSever
{
    internal class PrefixHandler
    {
        /// <summary>
        /// 消息头处理
        /// </summary>
        /// <param name="e"></param>
        /// <param name="receiveSendToken"></param>
        /// <param name="remainingBytesToProcess"></param>
        /// <returns></returns>
        internal int HandlePrefix(SocketAsyncEventArgs e, DataHoldingUserToken receiveSendToken,
            int remainingBytesToProcess)
        {
            if (receiveSendToken.PrefixByteDoneCount == 0)
            {
                receiveSendToken.ByteArrayForPrefix=new byte[receiveSendToken.PrefixLength];
            }

            if (remainingBytesToProcess >= receiveSendToken.PrefixLength - receiveSendToken.PrefixByteDoneCount)
            {
                Buffer.BlockCopy(e.Buffer,
                    receiveSendToken.ReceiveMessageOffset - receiveSendToken.PrefixLength +
                    receiveSendToken.PrefixByteDoneCount, receiveSendToken.ByteArrayForPrefix,
                    receiveSendToken.PrefixByteDoneCount,
                    receiveSendToken.PrefixLength - receiveSendToken.PrefixByteDoneCount);
                remainingBytesToProcess = remainingBytesToProcess - receiveSendToken.PrefixLength +
                                          receiveSendToken.PrefixByteDoneCount;
                receiveSendToken.RecPrefixBytesDoneThisOp = receiveSendToken.PrefixLength -
                                                            receiveSendToken.PrefixByteDoneCount;
                receiveSendToken.PrefixByteDoneCount = receiveSendToken.PrefixLength;
                receiveSendToken.LengthOfMessage = BitConverter.ToInt32(receiveSendToken.ByteArrayForPrefix, 0);
            }
            else
            {
                Buffer.BlockCopy(e.Buffer,
                    receiveSendToken.ReceiveMessageOffset - receiveSendToken.PrefixLength +
                    receiveSendToken.PrefixByteDoneCount, receiveSendToken.ByteArrayForPrefix,
                    receiveSendToken.PrefixByteDoneCount, remainingBytesToProcess);

                receiveSendToken.RecPrefixBytesDoneThisOp = remainingBytesToProcess;
                receiveSendToken.PrefixByteDoneCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }

            if (remainingBytesToProcess == 0)
            {
                receiveSendToken.ReceiveMessageOffset = receiveSendToken.ReceiveMessageOffset -
                                                        receiveSendToken.RecPrefixBytesDoneThisOp;
                receiveSendToken.RecPrefixBytesDoneThisOp = 0;
            }
            return remainingBytesToProcess;
        }
    }
}
