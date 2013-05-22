using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Extension methods on IEnumerable.
	/// </summary>
	static class EnumerableExtensions
	{
		/// <summary>
		/// Is this sequence equal to a list of items.
		/// </summary>
		public static bool SequenceEqual<T>(this IEnumerable<T> list, params T[] items)
		{
			return list.SequenceEqual((IEnumerable<T>)items);
		}
	}
}