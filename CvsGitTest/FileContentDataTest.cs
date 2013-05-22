/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the FileContentData class.
	/// </summary>
	[TestClass]
	public class FileContentDataTest
	{
		[TestMethod]
		public void Equals_SameInstance_ReturnsTrue()
		{
			var x = new FileContentData(new byte[] { 1, 2, 3, }, 2);

			Assert.IsTrue(x.Equals(x));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var x = new FileContentData(new byte[] { 1, 2, 3, }, 2);

			Assert.IsFalse(x.Equals(null));
		}

		[TestMethod]
		public void Equals_SameDataAndLength_ReturnsTrue()
		{
			var x = new FileContentData(new byte[] { 1, 2, 3, }, 2);
			var y = new FileContentData(new byte[] { 1, 2, }, 2);

			Assert.IsTrue(x.Equals(y));
			Assert.IsTrue(y.Equals(x));
		}

		[TestMethod]
		public void Equals_SameDataDifferentLength_ReturnsFalse()
		{
			var x = new FileContentData(new byte[] { 1, 2, 3, }, 2);
			var y = new FileContentData(new byte[] { 1, 2, 3, }, 3);

			Assert.IsFalse(x.Equals(y));
			Assert.IsFalse(y.Equals(x));
		}

		[TestMethod]
		public void Equals_DifferentData_ReturnsFalse()
		{
			var x = new FileContentData(new byte[] { 1, 2, 3, }, 4);
			var y = new FileContentData(new byte[] { 2, 3, 4, }, 3);

			Assert.IsFalse(x.Equals(y));
			Assert.IsFalse(y.Equals(x));
		}
	}
}
