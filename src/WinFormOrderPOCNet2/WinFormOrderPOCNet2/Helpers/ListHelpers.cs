using System;
using System.Collections.Generic;

// Définition d’un équivalent Func<T, TResult>
public delegate TKey MyFunc<T, TKey>(T arg);

public static class ListHelpers
{
	public static List<T> OrderBy<T, TKey>(List<T> source, MyFunc<T, TKey> keySelector) where TKey : IComparable<TKey>
	{
		if (source == null) throw new ArgumentNullException("source");
		if (keySelector == null) throw new ArgumentNullException("keySelector");

		List<T> result = new List<T>(source);

		// Implémentation d’un tri à bulles (bubble sort)
		for (int i = 0; i < result.Count - 1; i++)
		{
			for (int j = 0; j < result.Count - i - 1; j++)
			{
				TKey key1 = keySelector(result[j]);
				TKey key2 = keySelector(result[j + 1]);

				if (key1.CompareTo(key2) > 0)
				{
					T temp = result[j];
					result[j] = result[j + 1];
					result[j + 1] = temp;
				}
			}
		}

		return result;
	}
}
