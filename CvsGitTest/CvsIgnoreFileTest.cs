/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the CvsIgnoreFile class.
	/// </summary>
	[TestClass]
	public class CvsIgnoreFileTest
	{
		[TestMethod]
		public void IsIgnoreFile_MatchesLowerCase()
		{
			var f = new FileContent("dir/.cvsignore", FileContentData.Empty);
			Assert.IsTrue(CvsIgnoreFile.IsIgnoreFile(f));
		}

		[TestMethod]
		public void IsIgnoreFile_FileInRoot()
		{
			var f = new FileContent(".cvsignore", FileContentData.Empty);
			Assert.IsTrue(CvsIgnoreFile.IsIgnoreFile(f));
		}

		[TestMethod]
		public void IsIgnoreFile_MixedCase()
		{
			var f = new FileContent("dir/.CVSignore", FileContentData.Empty);
			Assert.IsTrue(CvsIgnoreFile.IsIgnoreFile(f));
		}

		[TestMethod]
		public void Rewrite_SimpleFileNames()
		{
			var cvs = new FileContent(".cvsignore", MakeContents("file1", "file2"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 2);
			Assert.AreEqual(contents[0], "/file1");
			Assert.AreEqual(contents[1], "/file2");
		}

		[TestMethod]
		public void Rewrite_DeletedFile()
		{
			var cvs = FileContent.CreateDeadFile(".cvsignore");
			Assert.IsTrue(cvs.IsDead, ".cvsignore file is dead");
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.IsTrue(git.IsDead, ".gitignore file is dead");
		}

		[TestMethod]
		public void Rewrite_MultipleEntriesPerLine()
		{
			var cvs = new FileContent(".cvsignore", MakeContents("file1 file2", "file3"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 3);
			Assert.AreEqual(contents[0], "/file1");
			Assert.AreEqual(contents[1], "/file2");
			Assert.AreEqual(contents[2], "/file3");
		}

		[TestMethod]
		public void Rewrite_BlankLine()
		{
			var cvs = new FileContent(".cvsignore", MakeContents("file1", "", "file2"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 2);
			Assert.AreEqual(contents[0], "/file1");
			Assert.AreEqual(contents[1], "/file2");
		}

		[TestMethod]
		public void Rewrite_SpaceInFilename()
		{
			var cvs = new FileContent(".cvsignore", MakeContents(@"file\ with\ spaces.txt"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 1);
			Assert.AreEqual(contents[0], @"/file\ with\ spaces.txt");
		}

		[TestMethod]
		public void Rewrite_EscapedBackslash()
		{
			var cvs = new FileContent(".cvsignore", MakeContents(@"dir\\file.txt"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 1);
			Assert.AreEqual(contents[0], @"/dir\\file.txt");
		}

		[TestMethod]
		public void Rewrite_LeadingSpace()
		{
			var cvs = new FileContent(".cvsignore", MakeContents(@"  file.txt"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 1);
			Assert.AreEqual(contents[0], @"/file.txt");
		}

		[TestMethod]
		public void Rewrite_MultipleSpacesBetween()
		{
			var cvs = new FileContent(".cvsignore", MakeContents("file1.txt \t file2.txt"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, ".gitignore");
			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 2);
			Assert.AreEqual(contents[0], "/file1.txt");
			Assert.AreEqual(contents[1], "/file2.txt");
		}

		[TestMethod]
		public void Rewrite_FileInSubdir()
		{
			var cvs = new FileContent("dir1/dir2/.cvsignore", MakeContents("file1"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			Assert.AreEqual(git.Name, "dir1/dir2/.gitignore");
		}

		[TestMethod]
		public void Rewrite_NegatedEntry()
		{
			var cvs = new FileContent("dir1/dir2/.cvsignore", MakeContents("file1", "!file2"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 2);
			Assert.AreEqual(contents[0], "/file1");
			Assert.AreEqual(contents[1], "!/file2");
		}

		[TestMethod]
		public void Rewrite_WildcardEntry()
		{
			var cvs = new FileContent("dir1/dir2/.cvsignore", MakeContents("file*.txt"));
			var git = CvsIgnoreFile.Rewrite(cvs);

			var contents = GetContents(git);
			Assert.AreEqual(contents.Length, 1);
			Assert.AreEqual(contents[0], "/file*.txt");
		}


		private FileContentData MakeContents(params string[] lines)
		{
			var data = lines.Aggregate(
					new StringBuilder(),
					(buf, line) => buf.AppendFormat("{0}{1}", line, Environment.NewLine),
					buf => buf.ToString());

			var bytes = Encoding.UTF8.GetBytes(data);
			return new FileContentData(bytes);
		}

		private string[] GetContents(FileContent file)
		{
			var stringData = Encoding.UTF8.GetString(file.Data.Data, 0, (int)file.Data.Length);
			Assert.IsTrue(stringData.EndsWith(Environment.NewLine), "Has trailing newline");

			// strip trailing newline so we don't return an extra line
			stringData = stringData.Remove(stringData.Length - 2);
			return stringData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
		}
	}
}
