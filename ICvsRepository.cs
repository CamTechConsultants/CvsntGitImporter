/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CvsGitConverter
{
	/// <summary>
	/// Access files in the CVS repository.
	/// </summary>
	interface ICvsRepository
	{
		/// <summary>
		/// Get the contents of a specific file revision from the repository.
		/// </summary>
		FileContent GetCvsRevision(FileRevision f);
	}
}