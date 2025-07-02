using System;
using System.Collections.Generic;

public static class ListExtensions
{
    // Create a single, reusable Random instance to ensure true randomness.
    private static Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            // Pick a random index from the remaining unshuffled elements (0 to n).
            int k = rng.Next(n + 1);

            // Swap the random element with the current element.
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}