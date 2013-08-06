/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	interface IConfig
	{
		/// <summary>
		/// Is debug output and logging enabled?
		/// </summary>
		bool Debug { get; }

		/// <summary>
		/// The directory in which debug logs are stored. Never null.
		/// </summary>
		string DebugLogDir { get; }

		/// <summary>
		/// Should we actually import the data?
		/// </summary>
		bool DoImport { get; }

		/// <summary>
		/// Do we need to create the CVS log file?
		/// </summary>
		bool CreateCvsLog { get; }

		/// <summary>
		/// The name of the CVS log file. Never null.
		/// </summary>
		string CvsLogFileName { get; }

		/// <summary>
		/// The path to the CVS sandbox. Not null.
		/// </summary>
		string Sandbox { get; }

		/// <summary>
		/// The path to the Git repository to create. Not null.
		/// </summary>
		string GitDir { get; }

		/// <summary>
		/// Gets any configuration options to apply to the new repository.
		/// </summary>
		IEnumerable<GitConfigOption> GitConfig { get; }

		/// <summary>
		/// Should we repack the git repository after import?
		/// </summary>
		bool Repack { get; }

		/// <summary>
		/// The path to the CVS cache, if specified, otherwise null.
		/// </summary>
		string CvsCache { get; }

		/// <summary>
		/// Gets the number of CVS processes to run.
		/// </summary>
		uint CvsProcesses { get; }

		/// <summary>
		/// The default domain for user e-mail addresses. Not null.
		/// </summary>
		string DefaultDomain { get; }

		/// <summary>
		/// A file containing user mappings, if provided, otherwise null.
		/// </summary>
		UserMap Users { get; }

		/// <summary>
		/// Gets the user to use for creating tags. Never null.
		/// </summary>
		User Nobody { get; }

		/// <summary>
		/// The branches to import "head-only" files for.
		/// </summary>
		IEnumerable<string> HeadOnlyBranches { get; }

		/// <summary>
		/// Should a file be imported?
		/// </summary>
		/// <remarks>Excludes files that are "head-only"</remarks>
		bool IncludeFile(string filename);

		/// <summary>
		/// Is a file a "head-only" file, i.e. one whose head revision only should be imported?
		/// </summary>
		bool IsHeadOnly(string filename);

		/// <summary>
		/// The number of missing files before we declare a tag to be "partial".
		/// </summary>
		int PartialTagThreshold { get; }

		/// <summary>
		/// The matcher for tags.
		/// </summary>
		InclusionMatcher TagMatcher { get; }

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		Renamer TagRename { get; }

		/// <summary>
		/// The tag to mark imports with.
		/// </summary>
		string MarkerTag { get; }

		/// <summary>
		/// A rule to translate branch names into branchpoint tag names if specified, otherwise null.
		/// </summary>
		RenameRule BranchpointRule { get; }

		/// <summary>
		/// The matcher for branches.
		/// </summary>
		InclusionMatcher BranchMatcher { get; }

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		Renamer BranchRename { get; }
	}
}
