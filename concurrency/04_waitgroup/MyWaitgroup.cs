using System.Threading;
using Concurrency.Common;

namespace Concurrency;

public class MyWaitgroup
{
    private int _slots;

    public void Add(int jobs)
    {
        while (true)
        {
            int cur = Volatile.Read(ref _slots);
            int next = cur + jobs;

            if (next < 0)
                throw new InvalidOperationException("Счетчик не может быть отрицательным");

            if (Interlocked.CompareExchange(ref _slots, next, cur) == cur)
            {
                if (next == 0 && cur != 0)
                    Futex.WakeAll(ref _slots);

                return;
            }
        }
    }

    public void Done()
    {
        Add(-1);
    }

    public void Wait()
    {
        while (true)
        {
            int count = Volatile.Read(ref _slots);

            if (count == 0)
                return;

            Futex.Wait(ref _slots, count);
        }
    }
}