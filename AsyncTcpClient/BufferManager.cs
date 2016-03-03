using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    internal class BufferManager
    {
        private int totalBytesInBufferBlock;

        private byte[] bufferBlock;

        private Stack<int> freeIndexPool;

        private int currentIndex;
        private int bufferBytesAllocateForEachSaea;

        public BufferManager(int totalBytes, int totalBufferBytesInEachSaeaObject)
        {
            this.totalBytesInBufferBlock = totalBytes;
            this.currentIndex = 0;
            this.bufferBytesAllocateForEachSaea = totalBufferBytesInEachSaeaObject;
            this.freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 初始化缓存区
        /// </summary>
        internal void InitBuffer()
        {
            this.bufferBlock = new byte[totalBytesInBufferBlock];
        }

        internal bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (this.freeIndexPool.Count > 0)
            {
                args.SetBuffer(this.bufferBlock, this.freeIndexPool.Pop(), this.bufferBytesAllocateForEachSaea);
            }
            else
            {
                if ((totalBytesInBufferBlock - this.bufferBytesAllocateForEachSaea) < this.currentIndex)
                {
                    return false;
                }
                args.SetBuffer(this.bufferBlock, this.currentIndex, this.bufferBytesAllocateForEachSaea);
                this.currentIndex += this.bufferBytesAllocateForEachSaea;
            }
            return true;
        }

        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            this.freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}
