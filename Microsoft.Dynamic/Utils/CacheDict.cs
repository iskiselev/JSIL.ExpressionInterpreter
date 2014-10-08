/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils {
    /// <summary>
    /// Provides a dictionary-like object used for caches which holds onto a maximum
    /// number of elements specified at construction time.
    /// 
    /// This class is not thread safe.
    /// </summary>
    public class CacheDict<TKey, TValue> {
        private readonly Dictionary<TKey, KeyInfo> _dict = new Dictionary<TKey, KeyInfo>();
        private readonly CustomLinkedList<TKey> _list = new CustomLinkedList<TKey>();
        private readonly int _maxSize;

        /// <summary>
        /// Creates a dictionary-like object used for caches.
        /// </summary>
        /// <param name="maxSize">The maximum number of elements to store.</param>
        public CacheDict(int maxSize) {
            _maxSize = maxSize;
        }

        /// <summary>
        /// Tries to get the value associated with 'key', returning true if it's found and
        /// false if it's not present.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value) {
            KeyInfo storedValue;
            if (_dict.TryGetValue(key, out storedValue))
            {
                CustomLinkedListNode<TKey> node = storedValue.List;
                if (node.Previous != null)
                {
                    // move us to the head of the list...
                    _list.Remove(node);
                    _list.AddFirst(node);
                }

                value = storedValue.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Adds a new element to the cache, replacing and moving it to the front if the
        /// element is already present.
        /// </summary>
        public void Add(TKey key, TValue value) {
            KeyInfo keyInfo;
            if (_dict.TryGetValue(key, out keyInfo)) {
                // remove original entry from the linked list
                _list.Remove(keyInfo.List);
            } else if (_list.Count == _maxSize) {
                // we've reached capacity, remove the last used element...
                CustomLinkedListNode<TKey> node = _list.Last;
                _list.RemoveLast();
                bool res = _dict.Remove(node.Value);
                Debug.Assert(res);
            }
            
            // add the new entry to the head of the list and into the dictionary
            CustomLinkedListNode<TKey> listNode = new CustomLinkedListNode<TKey>(key);
            _list.AddFirst(listNode);
            _dict[key] = new CacheDict<TKey, TValue>.KeyInfo(value, listNode);
        }

        /// <summary>
        /// Returns the value associated with the given key, or throws KeyNotFoundException
        /// if the key is not present.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public TValue this[TKey key] {
            get {
                TValue res;
                if (TryGetValue(key, out res)) {
                    return res;
                }
                throw new KeyNotFoundException();
            }
            set {
                Add(key, value);
            }
        }

        private struct KeyInfo {
            internal readonly TValue Value;
            internal readonly CustomLinkedListNode<TKey> List;

            internal KeyInfo(TValue value, CustomLinkedListNode<TKey> list)
            {
                Value = value;
                List = list;
            }
        }
    }

    internal class CustomLinkedList<T> : IEnumerable<T>
    {
        private CustomLinkedListNode<T> _firstNode;
        private CustomLinkedListNode<T> _lastNode;
        private int _count;
        public int Count { get { return _count; } }

        public void Remove(CustomLinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node is null");
            }

            if (node.List != this)
            {
                throw new InvalidOperationException("Node belongs to another list");
            }

            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            else
            {
                _firstNode = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
            else
            {
                _lastNode = node.Previous;
            }

            _count--;

            node.List = null;
            node.Next = null;
            node.Previous = null;
        }

        public CustomLinkedListNode<T> Last
        {
            get { return _lastNode; }
        }

        public void AddFirst(CustomLinkedListNode<T> node)
        {
            if (node.List != null)
            {
                throw new InvalidOperationException("Node belongs to another list");
            }

            if (_firstNode != null)
            {
                _firstNode.Previous = node;
            }

            node.List = this;
            node.Next = _firstNode;
            _count++;

            _firstNode = node;

            if (_lastNode == null)
            {
                _lastNode = node;
            }
        }

        public void RemoveLast()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("List empty");
            }

            var node = _lastNode;
            _lastNode = _lastNode.Previous;
            _lastNode.Next = null;
            node.List = null;
            node.Previous = null;

            _count--;

            if (_lastNode == null)
            {
                _firstNode = null;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var current = _firstNode;
            while (current != null)
            {
                yield return current.Value;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class CustomLinkedListNode<T>
    {
        public CustomLinkedListNode(T value)
        {
            Value = value;
        }

        public CustomLinkedListNode<T> Previous { get; internal set; }
        public CustomLinkedListNode<T> Next { get; internal set; }
        public CustomLinkedList<T> List { get; internal set; }

        public T Value { get; private set; }
    }
}
