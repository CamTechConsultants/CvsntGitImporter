/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using CTC.CvsntGitImporter;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the CvsRepositoryCache class.
	/// </summary>
	[TestClass]
	public class CvsRepositoryCacheTest
	{
		private TempDir m_temp;

		[TestInitialize]
		public void Setup()
		{
			m_temp = new TempDir();
		}

		[TestCleanup]
		public void Clearup()
		{
			m_temp.Dispose();
		}

		[TestMethod]
		public void Construct_CreatesDirectoryIfMissing()
		{
			var cacheDir = m_temp.GetPath("dir");
			var cache = new CvsRepositoryCache(cacheDir, MockRepository.GenerateStub<ICvsRepository>());

			Assert.IsTrue(Directory.Exists(cacheDir));
		}

		[TestMethod]
		public void GetCvsRevision_CallsUnderlyingIfFileMissing()
		{
			var f = new FileRevision(new FileInfo("file.txt"), Revision.Create("1.1"),
					mergepoint: Revision.Empty,
					time: DateTime.Now,
					author: "fred",
					commitId: "c1");

			var repo = MockRepository.GenerateMock<ICvsRepository>();
			repo.Expect(r => r.GetCvsRevision(f)).Return(new FileContent("file.txt", FileContentData.Empty));
			var cache = new CvsRepositoryCache(m_temp.Path, repo);
			cache.GetCvsRevision(f);

			repo.VerifyAllExpectations();
		}

		[TestMethod]
		public void GetCvsRevision_ReturnsExistingFileIfPresent()
		{
			var f = new FileRevision(new FileInfo("file.txt"), Revision.Create("1.1"),
					mergepoint: Revision.Empty,
					time: DateTime.Now,
					author: "fred",
					commitId: "c1");

			var contents = new FileContentData(new byte[] { 1, 2, 3, 4 }, 4);
			var repo1 = MockRepository.GenerateStub<ICvsRepository>();
			repo1.Stub(r => r.GetCvsRevision(f)).Return(new FileContent("file.txt", contents));
			var cache1 = new CvsRepositoryCache(m_temp.Path, repo1);
			cache1.GetCvsRevision(f);

			// create a second cache
			var repo2 = MockRepository.GenerateMock<ICvsRepository>();
			var cache2 = new CvsRepositoryCache(m_temp.Path, repo1);
			var data = cache2.GetCvsRevision(f);

			repo2.AssertWasNotCalled(r => r.GetCvsRevision(f));
			Assert.AreNotSame(data.Data, contents);
			Assert.IsTrue(data.Data.Equals(contents));
		}
	}
}