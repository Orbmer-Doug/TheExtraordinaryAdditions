using System;
using System.Collections.Generic;
using Terraria;

namespace TheExtraordinaryAdditions.Core.DataStructures;

// Because I dont like the one by terraria
public readonly struct WeightedDict<T>
{
    private readonly T[] items;
    private readonly float[] cumulativeWeights;
    private readonly float totalWeight;

    public WeightedDict(in Dictionary<T, float> dict)
    {
        if (dict == null || dict.Count == 0)
            throw new ArgumentException("Dictionary cannot be null or empty");

        items = new T[dict.Count];
        cumulativeWeights = new float[dict.Count];

        int index = 0;
        float cumulativeSum = 0f;
        foreach ((T item, float weight) in dict)
        {
            items[index] = item;
            cumulativeSum += weight;
            cumulativeWeights[index] = cumulativeSum;
            index++;
        }

        totalWeight = cumulativeSum;
        if (totalWeight <= 0)
            throw new InvalidOperationException("Total weight must be positive");
    }

    public T GetRandom()
    {
        float randomVal = (float)Main.rand.NextDouble() * totalWeight;
        int left = 0;
        int right = cumulativeWeights.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (cumulativeWeights[mid] < randomVal)
                left = mid + 1;
            else
                right = mid - 1;
        }

        int selectedIndex = Math.Min(left, items.Length - 1);
        return items[selectedIndex];
    }
}