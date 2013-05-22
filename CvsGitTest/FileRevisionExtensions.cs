/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Extension methods on FileRevision class.
	/// </summary>
	static class FileRevisionExtensions
	{
		public static FileRevision WithMessage(this FileRevision @this, string message)
		{
			@this.AddMessage(message);
			return @this;
		}
	}
}
