/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the FileRevision class.
	/// </summary>
	[TestClass]
	public class FileRevisionTest
	{
		[TestMethod]
		public void IsAddedOnAnotherBranch_True()
		{
			var file = new FileInfo("file");
			var r = file.CreateRevision("1.1", "c0", isDead: true).WithMessage("file file was initially added on branch blah");

			Assert.IsTrue(r.IsAddedOnAnotherBranch);
		}

		[TestMethod]
		public void IsAddedOnAnotherBranch_NotTrunkVersion_ReturnsFalse()
		{
			var file = new FileInfo("file");
			var r = file.CreateRevision("1.1.2.1", "c0", isDead: true).WithMessage("file file was initially added on branch blah");

			Assert.IsFalse(r.IsAddedOnAnotherBranch);
		}

		[TestMethod]
		public void IsAddedOnAnotherBranch_IncorrectMessage_ReturnsFalse()
		{
			var file = new FileInfo("file");
			var r = file.CreateRevision("1.1", "c0", isDead: true).WithMessage("some comment");

			Assert.IsFalse(r.IsAddedOnAnotherBranch);
		}
	}
}