using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Terraria;

namespace TheExtraordinaryAdditions.Core.Utilities;

public class BitmaskUtils
{
    /// <summary>
    /// Number of bits per <see cref="ulong"/> mask element (64)
    /// </summary>
    public const int BitsPerMask = sizeof(ulong) * 8;

    /// <summary>
    /// An enumerator for iterating over active indices in a bitmask
    /// </summary>
    public struct BitmaskEnumerator : IEnumerable<int>, IEnumerator<int>
    {
        private readonly ulong[] presenceMask;
        private readonly uint maxIndex;
        private int currentMaskIndex;
        private ulong currentMask;
        private int currentIndex;

        public BitmaskEnumerator(ulong[] presenceMask, uint maxIndex)
        {
            this.presenceMask = presenceMask;
            this.maxIndex = maxIndex;
            currentMaskIndex = -1;
            currentMask = 0;
            currentIndex = -1;
        }

        public BitmaskEnumerator GetEnumerator() => this;

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;

        public int Current => currentIndex;

        object IEnumerator.Current => currentIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (true)
            {
                // If current mask has bits left, process them
                if (currentMask != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(currentMask);
                    currentMask &= ~(1ul << bitIndex);
                    currentIndex = currentMaskIndex * 64 + bitIndex;
                    if (currentIndex < maxIndex)
                        return true;
                }
                else
                {
                    // Move to next mask
                    currentMaskIndex++;
                    if (currentMaskIndex >= presenceMask.Length)
                        return false;
                    currentMask = presenceMask[currentMaskIndex];
                }
            }
        }

        public void Reset()
        {
            currentMaskIndex = -1;
            currentMask = 0;
            currentIndex = -1;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Sets or clears a bit in the bitmask to mark a slot as active or inactive
    /// </summary>
    /// <param name="presenceMask">The bitmask array to modify</param>
    /// <param name="index">The slot index to set or clear</param>
    /// <param name="value"><c>true</c> for active, <c>false</c> for inactive</param>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(in ulong[] presenceMask, int index, bool value)
    {
        int maskIndex = index / 64;
        int bitIndex = index % 64;
        if (value)
            presenceMask[maskIndex] |= (1ul << bitIndex);
        else
            presenceMask[maskIndex] &= ~(1ul << bitIndex);
    }

    /// <summary>
    /// Allocates a free slot in the bitmask, marking it as active
    /// </summary>
    /// <param name="presenceMask">The bitmask array to modify</param>
    /// <param name="maxCount">The maximum number of slots available</param>
    /// <param name="overrideIndex">If <c>true</c>, overrides a random slot when no free slots are available; otherwise, returns -1</param>
    public static int AllocateIndex(in ulong[] presenceMask, uint maxCount, bool overrideIndex = false)
    {
        for (int maskIndex = 0, baseIndex = 0; maskIndex < presenceMask.Length; maskIndex++, baseIndex += BitsPerMask)
        {
            int bitIndex = BitOperations.TrailingZeroCount(~presenceMask[maskIndex]);
            if (bitIndex != BitsPerMask)
            {
                int index = baseIndex + bitIndex;
                if (index < maxCount)
                {
                    presenceMask[maskIndex] |= 1ul << bitIndex;
                    return index;
                }
            }
        }

        if (overrideIndex)
        {
            int randomIndex = Main.rand.Next((int)maxCount);
            presenceMask[randomIndex / BitsPerMask] |= 1ul << randomIndex % BitsPerMask;
            return randomIndex;
        }

        return -1;
    }

    /// <summary>
    /// Creates a new bitmask array for a given number of slots
    /// </summary>
    /// <param name="maxCount">The number of slots the bitmask should support</param>
    public static ulong[] CreateMask(uint maxCount)
    {
        return new ulong[Math.Max(1, (maxCount + BitsPerMask - 1) / BitsPerMask)];
    }

    /// <summary>
    /// Counts the number of active slots in the bitmask
    /// </summary>
    public static int CountActive(in ulong[] presenceMask)
    {
        int count = 0;
        for (int i = 0; i < presenceMask.Length; i++)
            count += BitOperations.PopCount(presenceMask[i]);
        return count;
    }

    /// <summary>
    /// Clears the bitmask, marking all slots as inactive
    /// </summary>
    /// <param name="presenceMask"></param>
    public static void Clear(in ulong[] presenceMask)
    {
        Array.Clear(presenceMask, 0, presenceMask.Length);
    }
}