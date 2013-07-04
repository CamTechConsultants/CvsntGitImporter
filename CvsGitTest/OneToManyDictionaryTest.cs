/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the OneToManyDictionary class.
	/// </summary>
	[TestClass]
	public class OneToManyDictionaryTest
	{
		[TestMethod]
		public void IndexerGet_KeyNotFound_ReturnsEmptyList()
		{
			var dict = new OneToManyDictionary<string, int>();
			var values = dict["blah"];

			Assert.AreEqual(values.Count(), 0);
		}

		[TestMethod]
		public void IndexerGet_KeyFound_ReturnsItems()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 2);
			dict.Add("key", 1);

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(2, 1));
		}

		[TestMethod]
		public void IndexerSet_NewKey_SetsItems()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict["key"] = new[] { 2, 3, 1 };

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(2, 3, 1));
		}

		[TestMethod]
		public void IndexerSet_ExistingKey_ReplacesItems()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict["key"] = new[] { 2, 3, 1 };

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(2, 3, 1));
		}

		[TestMethod]
		public void IndexerSet_ExistingKey_EmptyList_ClearsItems()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict["key"] = new int[0];

			var values = dict["key"];

			Assert.IsFalse(values.Any());
		}

		[TestMethod]
		public void Keys_EmptyCollection_ReturnsEmptyList()
		{
			var dict = new OneToManyDictionary<string, int>();
			var keys = dict.Keys;
			Assert.IsFalse(keys.Any());
		}

		[TestMethod]
		public void Keys_ReturnsKeys()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key2", 1);
			dict.Add("key2", 2);
			dict.Add("key1", 3);

			var keys = dict.Keys;

			Assert.IsTrue(keys.OrderBy(k => k).SequenceEqual("key1", "key2"));
		}

		[TestMethod]
		public void Add_NewKey_CreatesList()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(42));
		}

		[TestMethod]
		public void Add_ExistingKey_AppendsToList()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict.Add("key", 43);

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(42, 43));
		}

		[TestMethod]
		public void Add_ExistingKey_DuplicateValuesIgnored()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict.Add("key", 42);

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(42));
		}

		[TestMethod]
		public void AddRange_NewKey_CreatesList()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.AddRange("key", new[] { 42, 43 });

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(42, 43));
		}

		[TestMethod]
		public void AddRange_ExistingKey_AppendsToList()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict.AddRange("key", new[] { 43, 44 });

			var values = dict["key"];

			Assert.IsTrue(values.SequenceEqual(42, 43, 44));
		}

		[TestMethod]
		public void ContainsKey_KeyNotFound_ReturnsFalse()
		{
			var dict = new OneToManyDictionary<string, int>();
			Assert.IsFalse(dict.ContainsKey("key"));
		}

		[TestMethod]
		public void ContainsKey_KeyExists_ReturnsTrue()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			Assert.IsTrue(dict.ContainsKey("key"));
		}

		[TestMethod]
		public void Remove_KeyNotFound()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Remove("blah");
		}

		[TestMethod]
		public void Remove_KeyExists_RemovesAllItems()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("key", 42);
			dict.Add("key", 43);

			dict.Remove("key");

			Assert.IsFalse(dict.ContainsKey("key"));
		}

		[TestMethod]
		public void Count_Empty()
		{
			var dict = new OneToManyDictionary<string, int>();

			Assert.AreEqual(dict.Count, 0);
		}

		[TestMethod]
		public void Count()
		{
			var dict = new OneToManyDictionary<string, int>();
			dict.Add("x", 42);
			dict.Add("y", 66);

			Assert.AreEqual(dict.Count, 2);
		}
	}
}