using System;
using System.Collections;
using System.Collections.Generic;

namespace ModLib.Generics;

/// <summary>
///     A collection of weakly-referenced elements of a given type.
/// </summary>
/// <typeparam name="T">The type of this collection.</typeparam>
public class WeakCollection<T> : ICollection<T> where T : class
{
    /// <summary>
    ///     The internal list used for tracking <see cref="WeakReference{T}"/> objects.
    /// </summary>
    protected readonly List<WeakReference<T>> list = [];

    /// <summary>
    ///     Adds a new weak reference to the <see cref="WeakCollection{T}"/>.
    /// </summary>
    /// <param name="item">The object to be referenced.</param>
    public void Add(T item) => list.Add(new WeakReference<T>(item));

    /// <summary>
    ///     Removes all items from the <see cref="WeakCollection{T}"/>.
    /// </summary>
    public void Clear() => list.Clear();

    /// <summary>
    ///     Gets the number of weak references contained in the <see cref="WeakReference{T}"/>.
    /// </summary>
    /// <returns>The number of weak references contained in the <see cref="WeakReference{T}"/>.</returns>
    public int Count => list.Count;

    /// <summary>
    ///     Gets a value indicating whether the <see cref="WeakCollection{T}"/> is read-only.
    /// </summary>
    /// <returns><c>true</c> if the <see cref="WeakCollection{T}"/> is read-only; <c>false</c> otherwise.</returns>
    public bool IsReadOnly => false;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Determines whether the <see cref="WeakCollection{T}"/> contains a specific value.
    /// </summary>
    /// <returns><c>true</c> if <paramref name="item"/> is found in the <see cref="WeakCollection{T}"/>; <c>false</c> otherwise.</returns>
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

    /// <summary>
    ///     Copies the elements of the <see cref="WeakCollection{T}"/> to an Array, starting at a particular Array index.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements copied from <see cref="WeakCollection{T}"/>. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (T element in this)
        {
            array[arrayIndex++] = element;
        }
    }

    /// <summary>
    ///     Removes the first occurrence of a specific object from the <see cref="WeakCollection{T}"/>.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="WeakCollection{T}"/>.</param>
    /// <returns>
    ///     true if item was successfully removed from the <see cref="WeakCollection{T}"/>; otherwise, false.
    ///     This method also returns false if item is not found in the original <see cref="WeakCollection{T}"/>.
    /// </returns>
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

    /// <inheritdoc/>
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