﻿using System;
using System.Collections.Generic;
using UniRx;

namespace ObservablePriorityStack
{
    public abstract class PrioritizedObject<TEnum> : IComparable<PrioritizedObject<TEnum>> where TEnum : Enum
    {
        private TEnum _priorityValue;

        public PrioritizedObject(TEnum priorityValue)
        {
            _priorityValue = priorityValue;
        }

        public virtual int CompareTo(PrioritizedObject<TEnum> other)
        {
            return other._priorityValue.CompareTo(_priorityValue);
        }
    }

    public interface IObservablePriorityStackCurrent<T> where T : IComparable<T>
    {
        ReactiveProperty<T> Current { get; }
    }

    public interface IPriorityStack<T> where T : IComparable<T>
    {
        int Count { get; }
        T Peek();
        T Dequeue();
        void Enqueue(T item);
        bool Remove(T item);
    }

    public sealed class ObservablePriorityStack<T> : IObservablePriorityStackCurrent<T>, IPriorityStack<T> where T : IComparable<T>
    {
        public ReactiveProperty<T> Current { get; }
        private long _count = long.MinValue;
        private IndexedItem[] _items;
        private int _size;

        public ObservablePriorityStack()
            : this(16)
        {
        }

        public ObservablePriorityStack(int capacity)
        {
            Current = new ReactiveProperty<T>();
            _items = new IndexedItem[capacity];
            _size = 0;
        }

        private bool IsHigherPriority(int left, int right)
        {
            return _items[left].CompareTo(_items[right]) < 0;
        }

        private int Percolate(int index)
        {
            if (index >= _size || index < 0)
            {
                return index;
            }

            var parent = (index - 1) / 2;
            while (parent >= 0 && parent != index && IsHigherPriority(index, parent))
            {
                // swap index and parent
                var temp = _items[index];
                _items[index] = _items[parent];
                _items[parent] = temp;

                index = parent;
                parent = (index - 1) / 2;
            }

            return index;
        }

        private void Heapify(int index)
        {
            if (index >= _size || index < 0)
            {
                return;
            }

            while (true)
            {
                var left = 2 * index + 1;
                var right = 2 * index + 2;
                var first = index;

                if (left < _size && IsHigherPriority(left, first))
                {
                    first = left;
                }

                if (right < _size && IsHigherPriority(right, first))
                {
                    first = right;
                }

                if (first == index)
                {
                    break;
                }

                // swap index and first
                var temp = _items[index];
                _items[index] = _items[first];
                _items[first] = temp;

                index = first;
            }
        }

        public int Count => _size;

        public T Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("HEAP_EMPTY");
            }

            return _items[0].Value;
        }

        private void RemoveAt(int index)
        {
            _items[index] = _items[--_size];
            _items[_size] = default;

            if (Percolate(index) == index)
            {
                Heapify(index);
            }

            if (_size < _items.Length / 4)
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length / 2];
                Array.Copy(temp, 0, _items, 0, _size);
            }
        }

        public T Dequeue()
        {
            var result = Peek();
            RemoveAt(0);
            Current.Value = _items[0].Value;
            return result;
        }

        public void Enqueue(T item)
        {
            if (_size >= _items.Length)
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length * 2];
                Array.Copy(temp, _items, temp.Length);
            }

            var index = _size++;
            _items[index] = new IndexedItem {Value = item, Id = ++_count};
            Percolate(index);
            Current.Value = _items[0].Value;
        }

        public bool Remove(T item)
        {
            for (var i = 0; i < _size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(_items[i].Value, item))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private struct IndexedItem : IComparable<IndexedItem>
        {
            public T Value;
            public long Id;

            public int CompareTo(IndexedItem other)
            {
                var c = Value.CompareTo(other.Value);
                if (c == 0)
                {
                    c = other.Id.CompareTo(Id);
                }

                return c;
            }
        }
    }
}