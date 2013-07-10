/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Automatically resolve tags to single commits.
	/// </summary>
	interface ITagResolver
	{
		/// <summary>
		/// Gets a lookup that returns the resolved commits for each tag.
		/// </summary>
		IDictionary<string, Commit> ResolvedTags { get; }

		/// <summary>
		/// Gets a list of any unresolved tags.
		/// </summary>
		IEnumerable<string> UnresolvedTags { get; }

		/// <summary>
		/// Gets the (possibly re-ordered) list of commits.
		/// </summary>
		IEnumerable<Commit> Commits { get; }

		/// <summary>
		/// Resolve tags. Find what tags each commit contributes to and build a stack for each tag.
		/// The last commit that contributes to a tag should be the one that we tag.
		/// </summary>
		/// <returns>true if all tags are resolvable, otherwise false</returns>
		bool Resolve(IEnumerable<string> tags, IEnumerable<Commit> commits);
	}
}