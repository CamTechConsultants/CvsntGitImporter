/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the UnixTime class.
	/// </summary>
	[TestClass]
	public class UnixTimeTest
	{
		[TestMethod]
		public void FromDateTime_PureDate()
		{
			var date = new DateTime(2012, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);
			var unix = UnixTime.FromDateTime(date);

			Assert.AreEqual(unix, "1354320000 +0000");
		}

		[TestMethod]
		public void FromDateTime_DateAndTime()
		{
			var date = new DateTime(2012, 12, 1, 13, 45, 12, DateTimeKind.Unspecified);
			var unix = UnixTime.FromDateTime(date);

			Assert.AreEqual(unix, "1354369512 +0000");
		}

		[TestMethod]
		public void FromDateTime_DateAndTimeWithMilliseconds()
		{
			var date = new DateTime(2012, 12, 1, 13, 45, 12, 500, DateTimeKind.Unspecified);
			var unix = UnixTime.FromDateTime(date);

			Assert.AreEqual(unix, "1354369512 +0000");
		}
	}
}