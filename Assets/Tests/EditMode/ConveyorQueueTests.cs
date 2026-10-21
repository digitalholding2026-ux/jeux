using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class ConveyorQueueTests
    {
        [Test]
        public void DefaultCapacity_IsFive()
        {
            var queue = new ConveyorQueue<string>();

            Assert.AreEqual(5, queue.Capacity);
        }

        [Test]
        public void EnqueueDequeue_IsFifo()
        {
            var queue = new ConveyorQueue<string>();

            queue.TryEnqueue("a");
            queue.TryEnqueue("b");
            queue.TryEnqueue("c");

            Assert.IsTrue(queue.TryDequeue(out var first));
            Assert.AreEqual("a", first);
            Assert.IsTrue(queue.TryDequeue(out var second));
            Assert.AreEqual("b", second);
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void TryDequeue_WhenEmpty_ReturnsFalse()
        {
            var queue = new ConveyorQueue<string>();

            Assert.IsFalse(queue.TryDequeue(out _));
            Assert.IsTrue(queue.IsEmpty);
        }

        [Test]
        public void SixthItem_Overflows_AndRaisesEvent()
        {
            var queue = new ConveyorQueue<string>();
            string overflowed = null;
            queue.Overflow += item => overflowed = item;

            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(queue.TryEnqueue("item" + i));
            }

            Assert.IsTrue(queue.IsFull);
            Assert.IsFalse(queue.TryEnqueue("item5"));
            Assert.AreEqual("item5", overflowed);
            Assert.AreEqual(5, queue.Count);
        }

        [Test]
        public void ItemAdded_RaisesWithIndex()
        {
            var queue = new ConveyorQueue<string>();
            var indexes = new List<int>();
            queue.ItemAdded += (item, index) => indexes.Add(index);

            queue.TryEnqueue("a");
            queue.TryEnqueue("b");

            CollectionAssert.AreEqual(new[] { 0, 1 }, indexes);
        }

        [Test]
        public void TryRemove_RemovesSpecificItem_AndRaisesRemoved()
        {
            var queue = new ConveyorQueue<string>();
            string removed = null;
            queue.ItemRemoved += (item, index) => removed = item;

            queue.TryEnqueue("a");
            queue.TryEnqueue("b");
            queue.TryEnqueue("c");

            Assert.IsTrue(queue.TryRemove("b"));
            Assert.AreEqual("b", removed);
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual("a", queue[0]);
            Assert.AreEqual("c", queue[1]);
        }

        [Test]
        public void Clear_RemovesEverything_InFifoOrder()
        {
            var queue = new ConveyorQueue<string>();
            var removed = new List<string>();
            queue.ItemRemoved += (item, index) => removed.Add(item);

            queue.TryEnqueue("a");
            queue.TryEnqueue("b");
            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            CollectionAssert.AreEqual(new[] { "a", "b" }, removed);
        }
    }
}
