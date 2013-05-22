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
	}
}