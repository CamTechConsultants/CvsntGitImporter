/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.IO;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the GitRepo class.
	/// </summary>
	[TestClass]
	public class GitRepoTest
	{
		[TestMethod]
		public void Init_CreatesGitFiles()
		{
			using (var temp = new TempDir())
			{
				var git = new GitRepo(temp.Path);
				git.Init();

				Assert.IsTrue(Directory.Exists(temp.GetPath("refs")));
				Assert.IsTrue(File.Exists(temp.GetPath("HEAD")));
			}
		}

		[TestMethod]
		public void Init_CreatesDirectoryIfItDoesNotExist()
		{
			using (var temp = new TempDir())
			{
				var gitdir = temp.GetPath(@"dir1\dir2");
				var git = new GitRepo(gitdir);
				git.Init();

				Assert.IsTrue(Directory.Exists(gitdir));
				Assert.IsTrue(Directory.Exists(temp.GetPath(@"dir1\dir2\refs")));
			}
		}
	}
}