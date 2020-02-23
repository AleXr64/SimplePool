using System;
using System.Collections.Generic;

namespace AleXr64.SimplePool
{
    public class Pool<T> where T: class
    {
        private static readonly object _locker = new object();
        private readonly IBurstStrategy _burstStrategy;
        private readonly IPooledItemFactory<T> _fillStrategy;
        private Stack<T> _unused;
        private List<T> _used;

        public Pool(int startSize, IBurstStrategy burstStrategy, IPooledItemFactory<T> fillStrategy)
        {
            _burstStrategy = burstStrategy;
            Count = startSize;
            _fillStrategy = fillStrategy;
            Initialize();
        }

        public Pool(int startSize, IPooledItemFactory<T> fillStrategy)
        {
            _fillStrategy = fillStrategy;
            Count = startSize;
            Initialize();
        }

        public int Count { get; private set; }

        private void Initialize()
        {
            _unused = new Stack<T>(Count);
            _used = new List<T>(Count);
            var created = 0;
            while(created < Count)
            {
                _unused.Push(_fillStrategy.Instance());
                created++;
            }
        }

        public PoolEntry<T> GetEntry()
        {
            lock(_locker)
            {
                if(_unused.Count > 0)
                {
                    return MakeEntry();
                }

                if(Burst())
                {
                    return MakeEntry();
                }

                throw new NullReferenceException($"No free items of {typeof(T)} and cant burst");
            }
        }

        private PoolEntry<T> MakeEntry()
        {
            var value = _unused.Pop();
            _used.Add(value);
            return new PoolEntry<T>(this, value);
        }

        private bool Burst()
        {
            if(_burstStrategy == null)
            {
                return false;
            }

            var mustBurst = _burstStrategy.NeedUpscale(Count, out var newCount);

            if(mustBurst)
            {
                if(newCount > Count)
                {
                    Upscale(newCount);
                }
                else
                {
                    throw new Exception($"Burst straregy used for Pool of {typeof(T)} is wrong");
                }
            }

            return mustBurst;
        }

        private void Upscale(int newCount)
        {
            lock(_locker)
            {
                Count = newCount;
                var unused = _unused.ToArray();
                var used = _used.ToArray();
                Initialize();

                foreach(var value in unused)
                {
                    _unused.Push(value);
                }

                _used.AddRange(used);
            }
        }

        internal void Return(T value)
        {
            lock(_locker)
            {
                if(_used.Contains(value))
                {
                    _used.Remove(value);
                    _unused.Push(value);
                }
                else
                {
                    throw new ArgumentException($"Instance of {typeof(T)} not owned by pool");
                }
            }
        }
    }
}
