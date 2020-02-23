using System;

namespace AleXr64.SimplePool
{
    public class PoolEntry<T>: IDisposable where T: class
    {
        private readonly object _locker = new object();
        private T _value;
        private bool disposed;

        internal PoolEntry(Pool<T> owner, T value)
        {
            _owner = owner;
            _value = value;
        }

        private Pool<T> _owner { get; }

        public ref T Value {
            get
            {
                lock(_locker)
                {
                    if(disposed)
                    {
                        throw new ObjectDisposedException($"Pool entry for {typeof(T)} freed!");
                    }

                    return ref _value;
                }
            }
        }

        public void Dispose()
        {
            lock(_locker)
            {
                if(!disposed)
                {
                    _owner.Return(_value);
                    _value = null;
                    disposed = true;
                }
                else
                {
                    throw new ObjectDisposedException($"Pool entry for {typeof(T)} freed!");
                }
            }
        }
    }
}
