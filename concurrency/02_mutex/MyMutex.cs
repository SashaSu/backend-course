using System.Threading;
using Concurrency.Common;

namespace Concurrency;

public class MyMutex
{
    private const uint Free = 0;
    private const uint Held = 1;
    private const uint Contended = 2;

    private uint _state = Free;

    public bool TryLock()
    {
        return Interlocked.CompareExchange(ref _state, Held, Free) == Free;
    }

    public void Lock()
    {
        if (TryLock()) return;

        for (int i = 0; i < 100; i++)
        {
            if (TryLock()) return;
            Thread.SpinWait(4);
        }

        while (true)
        {
            uint state = Volatile.Read(ref _state);

            if (state == Held)
            {
                Interlocked.CompareExchange(ref _state, Contended, Held);
                continue;
            }

            if (state == Contended)
            {
                Futex.Wait(ref _state, Contended);
                continue;
            }

            if (Interlocked.CompareExchange(ref _state, Contended, Free) == Free) return;
        }
    }

    public void Unlock()
    {
        uint previous = Interlocked.Exchange(ref _state, Free);

        if (previous == Free)
            throw new SynchronizationLockException("Нельзя освободить незахваченный мьютекс");

        if (previous == Contended)
            Futex.Wake(ref _state);
    }
}