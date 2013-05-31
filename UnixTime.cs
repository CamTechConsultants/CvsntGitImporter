/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CTC.CvsntGitImporter
{
	static class UnixTime
	{
		/// <summary>
		/// Convert a .NET DateTime to a Unix time string.
		/// </summary>
		public static string FromDateTime(DateTime dateTime)
		{
			return String.Format("{0} +0000", (long)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
		}
	}
}