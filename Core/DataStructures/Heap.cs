﻿using System;
using System.Linq;

namespace TheExtraordinaryAdditions.Core.DataStructures;

public class Heap<T>(int maxHeapSize = 1000, int resizeBuffer = 100) where T : IHeapItem<T>
{
    private T[] items = new T[maxHeapSize];
    private int currentItemCount;
    private readonly int sizeBuffer = resizeBuffer;

    public void Add(T item)
    {
        if (currentItemCount >= items.Length)
            Resize();

        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T PopFirst()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);

        return firstItem;
    }

    public int Count => currentItemCount;

    public bool Contains(T item) => Equals(items[item.HeapIndex], item);

    public void UpdateItem(T item, bool sortDown = false)
    {
        SortUp(item);
        if (sortDown)
            SortDown(item);
    }

    public void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;
        while (true)
        {
            if (parentIndex == item.HeapIndex)
                break;

            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);

            else
                break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    public void SortDown(T item)
    {
        while (true)
        {
            int childIndexA = item.HeapIndex * 2 + 1;
            int childIndexB = item.HeapIndex * 2 + 2;
            if (childIndexA < currentItemCount)
            {
                int swapIndex = childIndexA;
                if (childIndexB < currentItemCount && items[childIndexA].CompareTo(items[childIndexB]) < 0)
                    swapIndex = childIndexB;

                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else
                    break;
            }
            else
                break;
        }
    }

    public void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        (itemB.HeapIndex, itemA.HeapIndex) = (itemA.HeapIndex, itemB.HeapIndex);
    }

    private void Resize()
    {
        T[] newItems = new T[items.Length + sizeBuffer];
        for (int i = 0; i < items.Length; i++)
            newItems[i] = items[i];

        items = newItems;
    }

    public bool TryFind(Func<T, bool> predicate, out T find)
    {
        find = default;
        if (Count == 0)
            return false;

        find = items.FirstOrDefault(predicate);
        return find != null;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}


/*

public class BinaryHeap
{
	private struct Tuple
	{
		public PathNode node;

		public uint F;

		public Tuple(uint f, PathNode node)
		{
			F = f;
			this.node = node;
		}
	}

	public int numberOfItems;

	public float growthFactor = 2f;

	private const int D = 4;

	private const bool SortGScores = true;

	public const ushort NotInHeap = ushort.MaxValue;

	private Tuple[] heap;

	public bool isEmpty => numberOfItems <= 0;

	private static int RoundUpToNextMultipleMod1(int v)
	{
		return v + (4 - (v - 1) % 4) % 4;
	}

	public BinaryHeap(int capacity)
	{
		capacity = RoundUpToNextMultipleMod1(capacity);
		heap = new Tuple[capacity];
		numberOfItems = 0;
	}

	public void Clear()
	{
		for (int i = 0; i < numberOfItems; i++)
		{
			heap[i].node.heapIndex = ushort.MaxValue;
		}
		numberOfItems = 0;
	}

	internal PathNode GetNode(int i)
	{
		return heap[i].node;
	}

	internal void SetF(int i, uint f)
	{
		heap[i].F = f;
	}

	private void Expand()
	{
		int num = RoundUpToNextMultipleMod1(Math.Max(heap.Length + 4, Math.Min(65533, (int)Math.Round((float)heap.Length * growthFactor))));
		if (num > 65534)
		{
			throw new Exception("Binary Heap Size really large (>65534). A heap size this large is probably the cause of pathfinding running in an infinite loop. ");
		}
		Tuple[] array = new Tuple[num];
		heap.CopyTo(array, 0);
		heap = array;
	}

	public void Add(PathNode node)
	{
		if (node == null)
		{
			throw new ArgumentNullException("node");
		}
		if (node.heapIndex != ushort.MaxValue)
		{
			DecreaseKey(heap[node.heapIndex], node.heapIndex);
			return;
		}
		if (numberOfItems == heap.Length)
		{
			Expand();
		}
		DecreaseKey(new Tuple(0u, node), (ushort)numberOfItems);
		numberOfItems++;
	}

	private void DecreaseKey(Tuple node, ushort index)
	{
		int num = index;
		uint num2 = (node.F = node.node.F);
		uint g = node.node.G;
		while (num != 0)
		{
			int num3 = (num - 1) / 4;
			if (num2 >= heap[num3].F && (num2 != heap[num3].F || g <= heap[num3].node.G))
			{
				break;
			}
			heap[num] = heap[num3];
			heap[num].node.heapIndex = (ushort)num;
			num = num3;
		}
		heap[num] = node;
		node.node.heapIndex = (ushort)num;
	}

	public PathNode Remove()
	{
		PathNode node = heap[0].node;
		node.heapIndex = ushort.MaxValue;
		numberOfItems--;
		if (numberOfItems == 0)
		{
			return node;
		}
		Tuple tuple = heap[numberOfItems];
		uint g = tuple.node.G;
		int num = 0;
		while (true)
		{
			int num2 = num;
			uint num3 = tuple.F;
			int num4 = num2 * 4 + 1;
			if (num4 <= numberOfItems)
			{
				uint f = heap[num4].F;
				uint f2 = heap[num4 + 1].F;
				uint f3 = heap[num4 + 2].F;
				uint f4 = heap[num4 + 3].F;
				if (num4 < numberOfItems && (f < num3 || (f == num3 && heap[num4].node.G < g)))
				{
					num3 = f;
					num = num4;
				}
				if (num4 + 1 < numberOfItems && (f2 < num3 || (f2 == num3 && heap[num4 + 1].node.G < ((num == num2) ? g : heap[num].node.G))))
				{
					num3 = f2;
					num = num4 + 1;
				}
				if (num4 + 2 < numberOfItems && (f3 < num3 || (f3 == num3 && heap[num4 + 2].node.G < ((num == num2) ? g : heap[num].node.G))))
				{
					num3 = f3;
					num = num4 + 2;
				}
				if (num4 + 3 < numberOfItems && (f4 < num3 || (f4 == num3 && heap[num4 + 3].node.G < ((num == num2) ? g : heap[num].node.G))))
				{
					num = num4 + 3;
				}
			}
			if (num2 == num)
			{
				break;
			}
			heap[num2] = heap[num];
			heap[num2].node.heapIndex = (ushort)num2;
		}
		heap[num] = tuple;
		tuple.node.heapIndex = (ushort)num;
		return node;
	}

	private void Validate()
	{
		for (int i = 1; i < numberOfItems; i++)
		{
			int num = (i - 1) / 4;
			if (heap[num].F > heap[i].F)
			{
				throw new Exception("Invalid state at " + i + ":" + num + " ( " + heap[num].F + " > " + heap[i].F + " ) ");
			}
			if (heap[i].node.heapIndex != i)
			{
				throw new Exception("Invalid heap index");
			}
		}
	}

	public void Rebuild()
	{
		for (int i = 2; i < numberOfItems; i++)
		{
			int num = i;
			Tuple tuple = heap[i];
			uint f = tuple.F;
			while (num != 1)
			{
				int num2 = num / 4;
				if (f >= heap[num2].F)
				{
					break;
				}
				heap[num] = heap[num2];
				heap[num].node.heapIndex = (ushort)num;
				heap[num2] = tuple;
				heap[num2].node.heapIndex = (ushort)num2;
				num = num2;
			}
		}
	}
}

*/