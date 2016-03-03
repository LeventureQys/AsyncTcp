using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    public class SocketAsyncEventArgsPool
    {
         private Stack<SocketAsyncEventArgs> pool;

        internal SocketAsyncEventArgsPool(int capacity)
        {
            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        internal int Count
        {
            get { return this.pool.Count; }
        }

        internal SocketAsyncEventArgs Pop()
        {
            lock (pool)
            {
                return this.pool.Pop();
            }
        }

        internal void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("添加SAEA对象不可以为空!");
            }
            lock (pool)
            {
                this.pool.Push(item);
            }
        }
    }
}
