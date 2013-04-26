/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
{
	/// <summary>
	/// Extension methods on FileInfo class.
	/// </summary>
	static class FileInfoExtensions
	{
		/// <summary>
		/// Add a tag or branch to a FileInfo.
		/// </summary>
		public static FileInfo WithTag(this FileInfo file, string tagName, string revision)
		{
			file.AddTag(tagName, revision);
			return file;
		}

		/// <summary>
		/// Add a branch to a FileInfo.
		/// </summary>
		public static FileInfo WithBranch(this FileInfo file, string branchName, string revision)
		{
			Assert.IsTrue(((Revision)revision).IsBranch);
			file.AddTag(branchName, revision);
			return file;
		}
	}
}