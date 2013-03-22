/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CvsGitConverter;
using System.Text.RegularExpressions;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the TagResolver class.
	/// </summary>
	[TestClass]
	public class TagResolverTest
	{
		[TestMethod]
		public void TagSplitAcrossCommits()
		{
			var file1 = new FileInfo("file1");
			file1.AddTag("tag", Revision.Create("1.2"));

			var file2 = new FileInfo("file2");
			file2.AddTag("tag", Revision.Create("1.2"));

			var id1 = "id1";
			var commit1 = new Commit(id1)
			{
				CreateFileRevision(file1, "1.1", id1),
				CreateFileRevision(file2, "1.1", id1),
			};
			var id2 = "id2";
			var commit2 = new Commit(id2)
			{
				CreateFileRevision(file1, "1.2", id2),
			};
			var id3 = "id3";
			var commit3 = new Commit(id3)
			{
				CreateFileRevision(file1, "1.3", id3),
				CreateFileRevision(file2, "1.2", id3),
			};

			var allFiles = new Dictionary<string, FileInfo>()
			{
				{ file1.Name, file1 },
				{ file2.Name, file2 },
			};

			var resolver = new TagResolver(new[] { commit1, commit2, commit3 }, allFiles, new InclusionMatcher());
			resolver.Resolve();

			Assert.IsTrue(Regex.IsMatch(resolver.Errors.Single(), @"No commit found for tag.*File: file1,r1\.3"));
		}


		private FileRevision CreateFileRevision(FileInfo file, string revision, string commitId)
		{
			return new FileRevision(file, Revision.Create(revision), Revision.Empty, DateTime.Now,
					"fred", commitId);
		}
	}
}
