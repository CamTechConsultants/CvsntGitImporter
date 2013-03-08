/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
{
	/// <summary>
	///
	/// </summary>
	[TestClass]
	public class RepositoryStateTest
	{
		[TestMethod]
		public void ApplyCommit_FilesUpdated()
		{
			var repoState = new RepositoryState("MAIN");

			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var id1 = "id1";
			var commit1 = new Commit(id1)
			{
				CreateFileRevision(file1, "1.1", id1),
				CreateFileRevision(file2, "1.1", id1),
			};

			var id2 = "id2";
			var commit2 = new Commit(id2)
			{
				CreateFileRevision(file2, "1.2", id2),
			};

			repoState.Apply(commit1);
			repoState.Apply(commit2);

			Assert.AreEqual(repoState[file1.Name], Revision.Create("1.1"));
			Assert.AreEqual(repoState[file2.Name], Revision.Create("1.2"));
		}

		[TestMethod]
		public void ApplyCommit_FileDeleted()
		{
			var repoState = new RepositoryState("MAIN");

			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var id1 = "id1";
			var commit1 = new Commit(id1)
			{
				CreateFileRevision(file1, "1.1", id1),
				CreateFileRevision(file2, "1.1", id1),
			};

			var id2 = "id2";
			var commit2 = new Commit(id2)
			{
				CreateFileRevision(file2, "1.2", id2, isDead: true),
			};

			repoState.Apply(commit1);
			repoState.Apply(commit2);

			Assert.AreEqual(repoState[file1.Name], Revision.Create("1.1"));
			Assert.AreEqual(repoState[file2.Name], Revision.Empty);
		}


		private FileRevision CreateFileRevision(FileInfo file, string revision, string commitId, bool isDead = false)
		{
			return new FileRevision(file, Revision.Create(revision), Revision.Empty, DateTime.Now,
					"fred", commitId, isDead: isDead);
		}
	}
}