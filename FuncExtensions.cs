/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Extension methods on Func.
	/// </summary>
	static class FuncExtensions
	{
		/// <summary>
		/// Memoize a function.
		/// </summary>
		public static Func<T, R> Memoize<T, R>(this Func<T, R> function)
		{
			return Memoize<T, R>(function, EqualityComparer<T>.Default);
		}

		/// <summary>
		/// Memoize a function with a custom key comparer.
		/// </summary>
		public static Func<T, R> Memoize<T, R>(this Func<T, R> function, IEqualityComparer<T> comparer)
		{
			var lookup = new Dictionary<T, R>(comparer);
			return x =>
			{
				R result;
				if (lookup.TryGetValue(x, out result))
				{
					return result;
				}
				else
				{
					result = function(x);
					lookup[x] = result;
					return result;
				}
			};
		}
	}
}