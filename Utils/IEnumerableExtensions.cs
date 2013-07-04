/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Extension methods for IEnumerable.
	/// </summary>
	static class IEnumerableExtensions
	{
		/// <summary>
		/// Perform the equivalent of String.Join on a sequence.
		/// </summary>
		/// <typeparam name="T">the type of the elements of source</typeparam>
		/// <param name="source">the sequence of values to join</param>
		/// <param name="separator">a string to insert between each item</param>
		/// <returns>a string containing the items in the sequence concenated and separated by the separator</returns>
		public static string StringJoin<T>(this IEnumerable<T> source, string separator)
		{
			return String.Join(separator, source);
		}

		/// <summary>
		/// Create a HashSet from a list of items.
		/// </summary>
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}

		/// <summary>
		/// Create a HashSet from a list of items.
		/// </summary>
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
		{
			return new HashSet<T>(source, comparer);
		}

		/// <summary>
		/// Remove repeated items from a list.
		/// </summary>
		public static IEnumerable<T> RemoveRepeats<T>(this IEnumerable<T> source)
		{
			return RemoveRepeats(source, EqualityComparer<T>.Default);
		}

		/// <summary>
		/// Remove repeated items from a list.
		/// </summary>
		public static IEnumerable<T> RemoveRepeats<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			else if (comparer == null)
				throw new ArgumentNullException("comparer");

			return RemoveRepeatsEnumerator(source, comparer);
		}

		private static IEnumerable<T> RemoveRepeatsEnumerator<T>(IEnumerable<T> source, IEqualityComparer<T> comparer)
		{
			bool first = true;
			T lastItem = default(T);

			foreach (var item in source)
			{
				if (first || !comparer.Equals(item, lastItem))
				{
					yield return item;
					lastItem = item;
					first = false;
				}
			}
		}
	}
}