using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    internal class DataHoldingUserToken
    {
        private DataHolder dataHolder;

        internal readonly int BufferOffset;

        /// <summary>
        /// 重置时使用
        /// </summary>
        internal readonly int PermanentReceiveMessagesOffset;

        /// <summary>
        ///头字节长度
        /// </summary>
        internal readonly int PrefixLength;

        /// <summary>
        /// 本次节接收操作的字节数
        /// </summary>
        private int recPrefixBytesDoneThisOp = 0;

        /// <summary>
        /// 头字节已完数
        /// </summary>
        private int prefixByteDoneCount = 0;

        /// <summary>
        /// 消息字节已完成数
        /// </summary>
        private int messageBytesDoneCount = 0;

        /// <summary>
        /// 标识接收主消息的消息偏移量
        /// </summary>
        private int receiveMessageOffset;

        /// <summary>
        /// 消息头字节
        /// </summary>
        private byte[] byteArrayForPrefix;

        private byte[] dataToSend;



        /// <summary>
        /// 当前接收/发送的消息的长度
        /// 在接收时指接到的消息长度
        /// 在发送时指，剩余要发送的长度。第一发次就指消息长度
        /// </summary>
        private int lengthOfMessage;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="prefixLength"></param>
        internal DataHoldingUserToken(int offset, int prefixLength)
        {
            this.BufferOffset = offset;
            this.PrefixLength = prefixLength;
            this.receiveMessageOffset = offset + prefixLength;
            this.PermanentReceiveMessagesOffset = this.receiveMessageOffset;
        }

        /// <summary>
        /// 创建接收数据区
        /// </summary>
        internal void CreateNewDataHolder()
        {
            dataHolder = new DataHolder();
        }

        /// <summary>
        /// 重置操作
        /// </summary>
        internal void Reset()
        {
            this.prefixByteDoneCount = 0;
            this.recPrefixBytesDoneThisOp = 0;
            this.MessageBytesDoneCount = 0;
            this.receiveMessageOffset = this.PermanentReceiveMessagesOffset;
        }


        internal DataHolder Holder
        {
            get { return dataHolder; }
            set { dataHolder = value; }
        }

        /// <summary>
        /// 当前接收/发送的消息的长度
        /// 在接收时指接到的消息长度
        /// 在发送时指，剩余要发送的长度。第一发次就指消息长度
        /// </summary>
        internal int LengthOfMessage
        {
            get { return lengthOfMessage; }
            set { lengthOfMessage = value; }
        }

        /// <summary>
        /// 标识接收主消息的消息偏移量
        /// </summary>
        internal int ReceiveMessageOffset
        {
            get { return receiveMessageOffset; }
            set { receiveMessageOffset = value; }
        }

        /// <summary>
        /// 头字节已完数
        /// </summary>
        internal int PrefixByteDoneCount
        {
            get { return prefixByteDoneCount; }
            set { prefixByteDoneCount = value; }
        }

        /// <summary>
        /// 消息字节已完成数
        /// </summary>
        internal int MessageBytesDoneCount
        {
            get { return messageBytesDoneCount; }
            set { messageBytesDoneCount = value; }
        }

        /// <summary>
        /// 本次节接收操作的字节数
        /// </summary>
        internal int RecPrefixBytesDoneThisOp
        {
            get { return recPrefixBytesDoneThisOp; }
            set { recPrefixBytesDoneThisOp = value; }
        }

        /// <summary>
        /// 要发送的字节数组
        /// </summary>
        internal byte[] DataToSend
        {
            get { return dataToSend; }
            set { dataToSend = value; }
        }

        /// <summary>
        /// 消息头字节
        /// </summary>
        public byte[] ByteArrayForPrefix
        {
            get { return byteArrayForPrefix; }
            set { byteArrayForPrefix = value; }
        }
    }
}
