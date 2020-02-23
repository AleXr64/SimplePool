namespace AleXr64.SimplePool
{
    public interface IPooledItemFactory<out T> where T: class
    {
        T Instance();
    }
}
