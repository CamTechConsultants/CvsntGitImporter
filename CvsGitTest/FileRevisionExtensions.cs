/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using CvsGitConverter;

namespace CvsGitTest
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
