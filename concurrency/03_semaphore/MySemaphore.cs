using System.Threading;
using Concurrency.Common;

namespace Concurrency;

public class MySemaphore
{
    private uint _slots;

    public MySemaphore(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        _slots = (uint)n;
    }

    public void Acquire()
    {
        while (true)
        {
            if (TryAcquire())
                return;

            Futex.Wait(ref _slots, 0);
        }
    }

    public bool TryAcquire()
    {
        while (true)
        {
            uint slots = Volatile.Read(ref _slots);

            if (slots == 0)
                return false;

            if (Interlocked.CompareExchange(ref _slots, slots - 1, slots) == slots)
                return true;
        }
    }

    public void Release()
    {
        while (true)
        {
            uint cur = Volatile.Read(ref _slots);

            if (Interlocked.CompareExchange(ref _slots, cur + 1, cur) == cur)
            {
                Futex.Wake(ref _slots);
                return;
            }
        }
    }

    public int Available()
    {
        return (int)Volatile.Read(ref _slots);
    }
}