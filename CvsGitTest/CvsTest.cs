/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using CTC.CvsntGitImporter.TestCode.Utils;
using System.Text;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the CVS class.
	/// </summary>
	[TestClass]
	public class CvsTest
	{
		[TestMethod]
		public void GetCommit_SingleFile()
		{
			var f1 = new FileInfo("file1.txt");
			var commit = new Commit("c1").WithRevision(f1, "1.1");

			var repo = MockRepository.GenerateStub<ICvsRepository>();
			repo.Stub(r => r.GetCvsRevision(null)).IgnoreArguments().Do((FileRevision f) => CreateMockContent(f));
			var cvs = new Cvs(repo, 1);

			var revisions = cvs.GetCommit(commit);
			Assert.IsTrue(Encoding.UTF8.GetString(revisions.Single().Data.Data) == "file1.txt r1.1");
		}

		[TestMethod]
		public void GetCommit_DeadFile()
		{
			var f1 = new FileInfo("file1.txt");
			var commit = new Commit("c1").WithRevision(f1, "1.1", isDead: true);

			var repo = MockRepository.GenerateStub<ICvsRepository>();
			var cvs = new Cvs(repo, 1);

			var revisions = cvs.GetCommit(commit);
			Assert.IsTrue(revisions.Single().IsDead);
		}

		[TestMethod]
		public void GetCommit_MultipleFiles()
		{
			var files = new FileInfo[6];
			var commit = new Commit("c1");

			for (int i = 0; i < files.Length; i++)
			{
				files[i] = new FileInfo(String.Format("file{0}.txt", i));
				commit.WithRevision(files[i], "1.1");
			}

			var repo = MockRepository.GenerateStub<ICvsRepository>();
			repo.Stub(r => r.GetCvsRevision(null)).IgnoreArguments().Do((FileRevision f) => CreateMockContent(f));
			var cvs = new Cvs(repo, (uint)(files.Length - 1));

			var revisions = cvs.GetCommit(commit).ToList();
			Assert.AreEqual(revisions.Count, files.Length);
			Assert.IsTrue(revisions.Select(r => r.Name).OrderBy(i => i).SequenceEqual(files.Select(f => f.Name)));
		}


		private static FileContent CreateMockContent(FileRevision f)
		{
			var data = Encoding.UTF8.GetBytes(String.Format("{0} r{1}", f.File.Name, f.Revision.ToString()));
			var content = new FileContentData(data, data.Length);
			return new FileContent(f.File.Name, content);
		}
	}
}
