using System;
using System.Collections.Generic;

namespace ModLib.Generics;

/// <summary>
///     A list of weakly-referenced values, which are removed when the underlying value is GC'ed.
/// </summary>
/// <typeparam name="T">The type of the elements of this list.</typeparam>
public class WeakList<T> : WeakCollection<T>, IList<T> where T : class
{
    /// <inheritdoc/>
    public T this[int index]
    {
        get => list[index].TryGetTarget(out T target) ? target : null!;
        set => list[index] = new WeakReference<T>(value);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        foreach (WeakReference<T> weakRef in list)
        {
            if (weakRef.TryGetTarget(out T target) && target == item)
            {
                return list.IndexOf(weakRef);
            }
        }

        return -1;
    }

    /// <inheritdoc/>
    public void Insert(int index, T item) => list.Insert(index, new WeakReference<T>(item));

    /// <inheritdoc/>
    public void RemoveAt(int index) => list.RemoveAt(index);
}