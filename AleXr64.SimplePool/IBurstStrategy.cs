namespace AleXr64.SimplePool
{
    public interface IBurstStrategy
    {
        bool NeedUpscale(int currentCount, out int newCount);
    }
}
