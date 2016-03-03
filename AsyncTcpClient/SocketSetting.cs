using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    public class SocketSetting
    {
        /// <summary>
        /// 创建多少个接收对象在接收池中
        /// </summary>
        private int numberOfSaeaForRec;

        /// <summary>
        /// 创建多少个发送对象在发送池中
        /// </summary>
        private int numberOfSaeaForSend;

        /// <summary>
        /// 队列中最大可等待的连接数。
        /// </summary>
        private int bakclog;

        //最大接收池
        private int maxSimultaneousAcceptOps;

        /// <summary>
        /// 头长度。
        /// </summary>
        private int prefixLength;

        private int bufferSize;

        private IPEndPoint localEndPoint;

        public SocketSetting(int bufferSize, int numberOfSaeaForRec, int numberOfSaeaForSend, int backlog, int maxSimultaneousAcceptOps,
            int prefixLength, IPEndPoint localEndPoint)
        {
            this.bufferSize = bufferSize;
            this.numberOfSaeaForRec = numberOfSaeaForRec;
            this.numberOfSaeaForSend = numberOfSaeaForSend;
            this.bakclog = backlog;
            this.maxSimultaneousAcceptOps = maxSimultaneousAcceptOps;
            this.prefixLength = prefixLength;

            this.localEndPoint = localEndPoint;
        }


        /// <summary>
        /// 创建多少个接收对象在接收池中
        /// </summary>
        public int NumberOfSaeaForRec
        {
            get { return numberOfSaeaForRec; }
        }

        /// <summary>
        /// 创建多少个发送对象在发送池中
        /// </summary>
        public int NumberOfSaeaForSend
        {
            get { return numberOfSaeaForSend; }
        }

        /// <summary>
        /// 队列中最大可等待的连接数。
        /// </summary>
        public int Bakclog
        {
            get { return bakclog; }
        }

        public int MaxSimultaneousAcceptOps
        {
            get { return maxSimultaneousAcceptOps; }
        }

        /// <summary>
        /// 头长度。
        /// </summary>
        public int PrefixLength
        {
            get { return prefixLength; }
        }


        public IPEndPoint LocalEndPoint
        {
            get { return localEndPoint; }
        }

        public int BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }
    }
}
