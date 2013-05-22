/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.IO;
using CTC.CvsntGitImporter;
using CTC.CvsntGitImporter.TestCode.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	[TestClass]
	public class UserMapTest
	{
		[TestMethod]
		[ExpectedException(typeof(IOException))]
		public void ParseUserFile_ExtraField()
		{
			using (var reader = new StringReader(UserMapResources.ExtraField))
			{
				var map = new UserMap("example.com");
				map.ParseUserFile(reader, "User File");
			}
		}

		[TestMethod]
		[ExpectedException(typeof(IOException))]
		public void ParseUserFile_MissingEmail()
		{
			using (var reader = new StringReader(UserMapResources.MissingEmail))
			{
				var map = new UserMap("example.com");
				map.ParseUserFile(reader, "User File");
			}
		}

		[TestMethod]
		[ExpectedException(typeof(IOException))]
		public void ParseUserFile_Duplicate()
		{
			using (var reader = new StringReader(UserMapResources.Duplicate))
			{
				var map = new UserMap("example.com");
				map.ParseUserFile(reader, "User File");
			}
		}

		[TestMethod]
		public void GetUser_NotFound()
		{
			var map = new UserMap("example.com");
			var user = map.GetUser("fred");

			Assert.AreEqual(user.Name, "fred");
			Assert.AreEqual(user.Email, "fred@example.com");
			Assert.IsTrue(user.Generated);
		}

		[TestMethod]
		public void GetUser_FromFile()
		{
			using (var reader = new StringReader(UserMapResources.Good))
			{
				var map = new UserMap("blah.com");
				map.ParseUserFile(reader, "User File");

				var joe = map.GetUser("joe");
				Assert.AreEqual(joe.Name, "Joe Bloggs");
				Assert.AreEqual(joe.Email, "joe.bloggs@example.com");
				Assert.IsFalse(joe.Generated);

				var fred = map.GetUser("fred");
				Assert.AreEqual(fred.Name, "Fred X");
				Assert.AreEqual(fred.Email, "fred2@example.com");
				Assert.IsFalse(fred.Generated);
			}
		}
	}
}