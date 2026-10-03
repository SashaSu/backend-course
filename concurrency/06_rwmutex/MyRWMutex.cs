using System.Threading;
using Concurrency.Common;

namespace Concurrency;

public class MyRWMutex
{
    private const uint WriterBit = 1u << 31;
    private const uint ReaderMask = ~WriterBit;

    private uint _state;
    private int _writersWaiting;

    public void RLock()
    {
        while (true)
        {
            uint state = Volatile.Read(ref _state);

            if ((state & WriterBit) != 0 || Volatile.Read(ref _writersWaiting) > 0)
            {
                Futex.Wait(ref _state, state);
                continue;
            }

            uint newState = state + 1;

            if (Interlocked.CompareExchange(ref _state, newState, state) == state)
                return;
        }
    }

    public void RUnlock()
    {
        while (true)
        {
            uint state = Volatile.Read(ref _state);

            if ((state & WriterBit) != 0)
                throw new SynchronizationLockException();

            uint readers = state & ReaderMask;

            if (readers == 0)
                throw new SynchronizationLockException();

            uint newState = state - 1;

            if (Interlocked.CompareExchange(ref _state, newState, state) == state)
            {
                if (readers == 1)
                    Futex.WakeAll(ref _state);

                return;
            }
        }
    }

    public void Lock()
    {
        Interlocked.Increment(ref _writersWaiting);

        while (true)
        {
            uint state = Volatile.Read(ref _state);

            if (state == 0)
            {
                if (Interlocked.CompareExchange(ref _state, WriterBit, 0) == 0)
                {
                    Interlocked.Decrement(ref _writersWaiting);
                    return;
                }

                continue;
            }

            Futex.Wait(ref _state, state);
        }
    }

    public void Unlock()
    {
        if (Interlocked.CompareExchange(ref _state, 0, WriterBit) != WriterBit)
            throw new SynchronizationLockException();

        Futex.WakeAll(ref _state);
    }
}