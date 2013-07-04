/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Extension methods on HashSet.
	/// </summary>
	static class HashSetExtensions
	{
		/// <summary>
		/// Add a list of values to a HashSet.
		/// </summary>
		public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> values)
		{
			foreach (var value in values)
				hashSet.Add(value);
		}
	}
}