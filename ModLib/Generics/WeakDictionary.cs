using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

// Credits to N73k for original script

namespace ModLib.Generics;

/// <summary>
///     A dictionary of weak references, akin to a <see cref="ConditionalWeakTable{T, V}"/>.
///     When a key or value is dropped/disposed/GC'ed, its corresponding pair is also removed.
/// </summary>
/// <typeparam name="TKey">The type for the keys of this dictionary.</typeparam>
/// <typeparam name="TValue">The type for the values of this dictionary.</typeparam>
public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    where TKey : class
    where TValue : class
{
    private readonly object locker = new();
    private ConditionalWeakTable<TKey, WeakKeyHolder> keyHolderMap = new();
    private Dictionary<WeakReference, TValue> valueMap = new(new ObjectReferenceEqualityComparer<WeakReference>());

    private class WeakKeyHolder(WeakDictionary<TKey, TValue> outer, TKey key)
    {
        private readonly WeakDictionary<TKey, TValue> outer = outer;

        public WeakReference WeakRef { get; private set; } = new WeakReference(key);

        ~WeakKeyHolder()
        {
            outer?.OnKeyDrop(WeakRef);  // Nullable operator used just in case this.outer gets set to null by GC before this finalizer runs. But I haven't had this happen.
        }
    }

    private void OnKeyDrop(WeakReference weakKeyRef)
    {
        lock (locker)
        {
            if (!bAlive)
                return;

            valueMap.Remove(weakKeyRef);
        }
    }

    // The reason for this is in case (for some reason which I have never seen) the finalizer trigger doesn't work
    // There is not much performance penalty with this, since this is only called in cases when we would be enumerating the inner collections anyway.
    private void ManualShrink()
    {
        List<WeakReference> keysToRemove = [.. valueMap.Keys.Where(k => !k.IsAlive)];

        foreach (WeakReference key in keysToRemove)
            valueMap.Remove(key);
    }

    private Dictionary<TKey, TValue> CurrentDictionary
    {
        get
        {
            lock (locker)
            {
                ManualShrink();
                return valueMap.ToDictionary(p => (TKey)p.Key.Target, p => p.Value);
            }
        }
    }

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue val) ? val : throw new KeyNotFoundException();

        set => Set(key, value, isUpdateOkay: true);
    }

    private bool Set(TKey key, TValue val, bool isUpdateOkay)
    {
        lock (locker)
        {
            if (keyHolderMap.TryGetValue(key, out WeakKeyHolder weakKeyHolder))
            {
                if (!isUpdateOkay)
                    return false;

                valueMap[weakKeyHolder.WeakRef] = val;
                return true;
            }

            weakKeyHolder = new WeakKeyHolder(this, key);
            keyHolderMap.Add(key, weakKeyHolder);
            //this.weakKeySet.Add(weakKeyHolder.WeakRef);
            valueMap.Add(weakKeyHolder.WeakRef, val);

            return true;
        }
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys
    {
        get
        {
            lock (locker)
            {
                ManualShrink();
                return [.. valueMap.Keys.Select(k => (TKey)k.Target)];
            }
        }
    }

    /// <inheritdoc/>
    public ICollection<TValue> Values
    {
        get
        {
            lock (locker)
            {
                ManualShrink();
                return [.. valueMap.Select(p => p.Value)];
            }
        }
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            lock (locker)
            {
                ManualShrink();
                return valueMap.Count;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public void Add(TKey key, TValue value)
    {
        if (!Set(key, value, isUpdateOkay: false))
            throw new ArgumentException("Key already exists");
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public void Clear()
    {
        lock (locker)
        {
            keyHolderMap = new ConditionalWeakTable<TKey, WeakKeyHolder>();
            valueMap.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        object? curVal = null;

        lock (locker)
        {
            if (!keyHolderMap.TryGetValue(item.Key, out WeakKeyHolder weakKeyHolder))
                return false;

            curVal = weakKeyHolder.WeakRef.Target;
        }

        return curVal?.Equals(item.Value) == true;
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        lock (locker)
        {
            return keyHolderMap.TryGetValue(key, out WeakKeyHolder weakKeyHolder);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)CurrentDictionary).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => CurrentDictionary.GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(TKey key)
    {
        lock (locker)
        {
            if (!keyHolderMap.TryGetValue(key, out WeakKeyHolder weakKeyHolder))
                return false;

            keyHolderMap.Remove(key);
            valueMap.Remove(weakKeyHolder.WeakRef);

            return true;
        }
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        lock (locker)
        {
            if (!keyHolderMap.TryGetValue(item.Key, out WeakKeyHolder weakKeyHolder))
                return false;

            if (weakKeyHolder.WeakRef.Target?.Equals(item.Value) != true)
                return false;

            keyHolderMap.Remove(item.Key);
            valueMap.Remove(weakKeyHolder.WeakRef);

            return true;
        }
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (locker)
        {
            if (!keyHolderMap.TryGetValue(key, out WeakKeyHolder weakKeyHolder))
            {
                value = default!;
                return false;
            }

            value = valueMap[weakKeyHolder.WeakRef];
            return true;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool bAlive = true;

    /// <inheritdoc/>
    public void Dispose() => Dispose(true);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    protected void Dispose(bool bManual)
    {
        if (bManual)
        {
            Monitor.Enter(locker);

            if (!bAlive)
                return;
        }

        try
        {
            keyHolderMap = null!;
            valueMap = null!;
            bAlive = false;
        }
        finally
        {
            if (bManual)
                Monitor.Exit(locker);
        }
    }

    ~WeakDictionary()
    {
        Dispose(false);
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private class ObjectReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        public static ObjectReferenceEqualityComparer<T> Default = new();

        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private class ObjectReferenceEqualityComparer : ObjectReferenceEqualityComparer<object>
    {
    }
}