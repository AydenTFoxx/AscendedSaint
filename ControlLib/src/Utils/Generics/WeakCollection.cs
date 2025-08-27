using System;
using System.Collections;
using System.Collections.Generic;

namespace ControlLib.Utils.Generics;

public class WeakCollection<T> : ICollection<T> where T : class
{
    protected readonly List<WeakReference<T>> list = [];

    public void Add(T item) => list.Add(new WeakReference<T>(item));
    public void Clear() => list.Clear();
    public int Count => list.Count;
    public bool IsReadOnly => false;
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(T item)
    {
        foreach (T element in this)
        {
            if (Equals(element, item))
            {
                return true;
            }
        }
        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (T element in this)
        {
            array[arrayIndex++] = element;
        }
    }

    public bool Remove(T item)
    {
        foreach (WeakReference<T> weakRef in list)
        {
            if (weakRef.TryGetTarget(out T target) && target == item)
            {
                return list.Remove(weakRef);
            }
        }
        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].TryGetTarget(out T element))
            {
                list.RemoveAt(i);
                continue;
            }
            yield return element;
        }
    }
}