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
		public void GetTags_RevisionTagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			var tags = file.GetTags(Revision.Create("1.1"));
			Assert.AreEqual(tags.Single(), "tag");
		}

		[TestMethod]
		public void GetTags_RevisionUntagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			Assert.IsFalse(file.GetTags(Revision.Create("1.2")).Any());
		}
	}
}
