using System;
using System.Collections.Generic;

namespace ColorFlow.Core
{
    /// <summary>
    /// File FIFO à capacité fixe (5 par défaut) représentant le tapis roulant.
    /// L'ajout sur une file pleine déclenche <see cref="Overflow"/> et l'élément est refusé.
    /// </summary>
    public sealed class ConveyorQueue<T>
    {
        public const int DefaultCapacity = 5;

        private readonly List<T> _items = new List<T>(DefaultCapacity);

        public ConveyorQueue(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "La capacité doit être strictement positive.");
            }

            Capacity = capacity;
        }

        /// <summary>Élément ajouté + index qu'il occupe dans la file.</summary>
        public event Action<T, int> ItemAdded;

        /// <summary>Élément retiré (consommé ou supprimé).</summary>
        public event Action<T, int> ItemRemoved;

        /// <summary>Un élément a été refusé parce que la file est pleine.</summary>
        public event Action<T> Overflow;

        public int Capacity { get; }

        public int Count => _items.Count;

        public bool IsFull => _items.Count >= Capacity;

        public bool IsEmpty => _items.Count == 0;

        public IReadOnlyList<T> Items => _items;

        public T this[int index] => _items[index];

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public bool TryEnqueue(T item)
        {
            if (IsFull)
            {
                Overflow?.Invoke(item);
                return false;
            }

            _items.Add(item);
            ItemAdded?.Invoke(item, _items.Count - 1);
            return true;
        }

        public bool TryDequeue(out T item)
        {
            return TryRemoveAt(0, out item);
        }

        public bool TryPeek(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[0];
            return true;
        }

        public bool TryRemoveAt(int index, out T item)
        {
            if (index < 0 || index >= _items.Count)
            {
                item = default;
                return false;
            }

            item = _items[index];
            _items.RemoveAt(index);
            ItemRemoved?.Invoke(item, index);
            return true;
        }

        /// <summary>
        /// Retire un élément précis (une créature tapée n'est pas forcément en tête).
        /// </summary>
        public bool TryRemove(T item)
        {
            var index = _items.IndexOf(item);
            return index >= 0 && TryRemoveAt(index, out _);
        }

        /// <summary>Vide la file en notifiant chaque retrait (utilisé au restart).</summary>
        public void Clear()
        {
            while (_items.Count > 0)
            {
                TryRemoveAt(0, out _);
            }
        }
    }
}
