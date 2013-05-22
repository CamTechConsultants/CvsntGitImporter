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
		public static Dictionary<string, FileInfo> CreateAllFiles(this IEnumerable<Commit> commits)
		{
			var allFiles = new Dictionary<string, FileInfo>();

			foreach (var f in commits.SelectMany(c => c.Select(r => r.File)).Distinct())
				allFiles.Add(f.Name, f);

			return allFiles;
		}
	}
}