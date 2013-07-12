/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the FileContent class.
	/// </summary>
	[TestClass]
	public class FileContentTest
	{
		[TestMethod]
		public void EmptyFile()
		{
			var data = new FileContentData(new byte[0]);
			var file = new FileContent("file", data);

			Assert.AreEqual("file", file.Name);
			Assert.AreEqual(0, file.Data.Length);
			Assert.IsFalse(file.IsDead);
		}

		[TestMethod]
		public void DeadFile()
		{
			var file = FileContent.CreateDeadFile("file");
			Assert.IsTrue(file.IsDead);
			Assert.AreEqual("file", file.Name);
		}
	}
}