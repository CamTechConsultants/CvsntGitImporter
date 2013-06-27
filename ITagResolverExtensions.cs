/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Extension methods on ITagResolver.
	/// </summary>
	static class ITagResolverExtensions
	{
		/// <summary>
		/// Gets a sorted list of resolved tags/branches.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> ResolvedTags(this ITagResolver resolver)
		{
			return resolver.ResolvedTags.Keys.OrderBy(t => t);
		}
	}
}