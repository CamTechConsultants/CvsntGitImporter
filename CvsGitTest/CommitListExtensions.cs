/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;
using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Extension methods on lists of commits.
	/// </summary>
	static class CommitListExtensions
	{
		public static FileCollection CreateAllFiles(this IEnumerable<Commit> commits, params FileInfo[] extraFiles)
		{
			return new FileCollection(commits.SelectMany(c => c.Select(r => r.File)).Concat(extraFiles).Distinct());
		}
	}
}