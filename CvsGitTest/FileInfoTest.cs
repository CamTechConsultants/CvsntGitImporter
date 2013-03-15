/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CvsGitConverter;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the FileInfo class.
	/// </summary>
	[TestClass]
	public class FileInfoTest
	{
		[TestMethod]
		public void GetTagsForRevision_RevisionTagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			var tags = file.GetTagsForRevision(Revision.Create("1.1"));
			Assert.AreEqual(tags.Single(), "tag");
		}

		[TestMethod]
		public void GetTagsForRevision_RevisionUntagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			Assert.IsFalse(file.GetTagsForRevision(Revision.Create("1.2")).Any());
		}

		[TestMethod]
		public void GetRevisionForTag_TagExists()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			var r = file.GetRevisionForTag("tag");
			Assert.AreEqual(r, Revision.Create("1.1"));
		}

		[TestMethod]
		public void GetRevisionForTag_TagDoesNotExist()
		{
			var file = new FileInfo("file.txt");

			var r = file.GetRevisionForTag("tag");
			Assert.AreEqual(r, Revision.Empty);
		}
	}
}
