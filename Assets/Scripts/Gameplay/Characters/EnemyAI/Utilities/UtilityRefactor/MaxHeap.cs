using System;
using System.Collections.Generic;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor
{
    public class MaxHeap<T>
    {
        private readonly List<KeyValuePair<float, T>> _elements = new();

        public int Count => _elements.Count;
        
        public void Clear()
        {
            _elements.Clear();
        }

        public void Add(float priority, T item)
        {
            _elements.Add(new KeyValuePair<float, T>(priority, item));
            HeapifyUp(_elements.Count - 1);
        }

        public T Peek()
        {
            return _elements[0].Value;
        }

        public T ExtractMax()
        {
            if (_elements.Count == 0) throw new InvalidOperationException("The heap is empty");

            T result = _elements[0].Value;

            _elements[0] = _elements[_elements.Count - 1];
            _elements.RemoveAt(_elements.Count - 1);

            HeapifyDown(0);

            return result;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (_elements[index].Key <= _elements[parentIndex].Key)
                    break;

                var temp = _elements[index];
                _elements[index] = _elements[parentIndex];
                _elements[parentIndex] = temp;

                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (index < _elements.Count)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;

                if (leftChildIndex >= _elements.Count)
                    break;

                int largestChildIndex = leftChildIndex;

                if (rightChildIndex < _elements.Count && _elements[rightChildIndex].Key > _elements[leftChildIndex].Key)
                {
                    largestChildIndex = rightChildIndex;
                }

                if (_elements[largestChildIndex].Key <= _elements[index].Key)
                    break;

                var temp = _elements[index];
                _elements[index] = _elements[largestChildIndex];
                _elements[largestChildIndex] = temp;

                index = largestChildIndex;
            }
        }
    }

}