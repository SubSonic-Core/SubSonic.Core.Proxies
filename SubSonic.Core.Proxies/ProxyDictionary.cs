using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace SubSonic.Core.Proxies
{
    public sealed class ProxyDictionary<TKey, TValue>
        : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, ConcurrentQueue<TValue>> dictionary;
        private readonly int concurrencyLevel;
        private readonly int size;
        private bool disposed;

        public ProxyDictionary()
        {
            concurrencyLevel = Environment.ProcessorCount * 8;
            size = concurrencyLevel * concurrencyLevel;
            dictionary = new ConcurrentDictionary<TKey, ConcurrentQueue<TValue>>(concurrencyLevel, size);
        }

        public void Add(TKey key, TValue value)
        {
            EnsureKey(key);

            if (TryGetValue(key, out ConcurrentQueue<TValue> q))
            {
                q.Enqueue(value);
            }
            else
            {
                throw new ArgumentException(ProxyResources.CouldNotAddValueToQueue.Format(value.GetType()));
            }
        }

        private void EnsureKey(TKey key)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.TryAdd(key, new ConcurrentQueue<TValue>());
            }
        }

        private bool TryGetValue(TKey key, out ConcurrentQueue<TValue> values)
        {
            return dictionary.TryGetValue(key, out values);
        }

        public int Count(TKey key)
        {
            EnsureKey(key);

            if (TryGetValue(key, out ConcurrentQueue<TValue> q))
            {
                return q.Count;
            }
            return default;
        }

        public TValue Request(TKey key, Func<TValue> builder = null)
        {
            EnsureKey(key);

            if(TryGetValue(key,out ConcurrentQueue<TValue> q))
            {
                if (q.TryDequeue(out TValue value))
                {
                    return value;
                }
                if (builder != null)
                {
                    return builder();
                }
            }

            return default;
        }

        public void Release(TKey key, TValue value)
        {
            Add(key, value);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach(var kvp in dictionary)
                    {
                        while(!kvp.Value.IsEmpty)
                        {
                            if (kvp.Value.TryDequeue(out TValue value) &&
                                value is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProxyDictionary()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
