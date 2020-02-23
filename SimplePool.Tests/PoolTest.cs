using System;
using System.Collections.Generic;
using AleXr64.SimplePool;
using Xunit;

namespace SimplePool.Tests
{
    public class PoolTest
    {
        private Pool<TestEnt> _pool;
        private IBurstStrategy _burstStrategy;
        private IPooledItemFactory<TestEnt> _fillStrategy;
        List<PoolEntry<TestEnt>> result;
        private void PrepareInterfaces()
        {
            _burstStrategy = new BurstStrategy();
            _fillStrategy = new FillStrategy();
        }

        private void CreatePoolWithoutStrategy(int count) { _pool = new Pool<TestEnt>(count, _fillStrategy); }
        private void CreatePoolWithStrategy(int count) { _pool = new Pool<TestEnt>(count, _burstStrategy, _fillStrategy);}

        [Fact]
        public void CanTake()
        {
            PrepareInterfaces();
            CreatePoolWithoutStrategy(5);
            
            result = new List<PoolEntry<TestEnt>>();

            for(var i = 1; i <= 5; i++)
            {
                result.Add(_pool.GetEntry());
            }

            Assert.True(result.Count == 5);
            Assert.Throws<NullReferenceException>(() => _pool.GetEntry());
        }

        [Fact]
        public void IsBursted()
        {
            PrepareInterfaces();
            CreatePoolWithStrategy(5);

           result= new List<PoolEntry<TestEnt>>();

            for (var i = 1; i <= 5; i++)
            {
                result.Add(_pool.GetEntry());
            }

            Assert.True(result.Count == 5);
            Assert.IsType<PoolEntry<TestEnt>>(_pool.GetEntry());
        }

        [Fact]
        public void TestDispose()
        {
            PrepareInterfaces();
            CreatePoolWithStrategy(5);

            result = new List<PoolEntry<TestEnt>>();

            for (var i = 1; i <= 5; i++)
            {
                result.Add(_pool.GetEntry());
            }

            Assert.True(result.Count == 5);
            foreach(var poolEntry in result)
            {
                poolEntry.Dispose();
            }
        }

    }

    public class TestEnt
    {
        public int Value;
    }

    public class BurstStrategy: IBurstStrategy
    {
        public bool NeedUpscale(int currentCount, out int newCount)
        {
            newCount = currentCount * 3;
            return true;
        }
    }

    public class FillStrategy: IPooledItemFactory<TestEnt>
    {
        public TestEnt Instance() => new TestEnt();
    }
}
