﻿/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CvsGitConverter.Utils;

namespace CvsGitConverter
{
	class Program
	{
		static readonly DateTime StartDate = new DateTime(2003, 2, 1);

		static void Main(string[] args)
		{
			var switches = new Switches();
			switches.Parse(args);

			if (switches.ExtraArguments.Count != 1)
				throw new ArgumentException("Need a cvs.log file");

			using (var log = new Logger("gitconvert.log"))
			{
				var parser = new CvsLogParser(switches.ExtraArguments[0], startDate: StartDate);
				var builder = new CommitBuilder(parser);
				IEnumerable<Commit> commits = builder.GetCommits()
						.SplitMultiBranchCommits()
						.AddCommitsToFiles()
						.ToListIfNeeded();

				Verify(commits);

				// build lookup of all files
				var allFiles = new Dictionary<string, FileInfo>();
				foreach (var f in parser.Files)
					allFiles.Add(f.Name, f);

				var branchResolver = new BranchResolver(log, commits, allFiles, switches.BranchMatcher);
				if (!branchResolver.ResolveAndFix())
				{
					throw new ImportFailedException(String.Format("Unable to resolve all branches to a single commit: {0}",
							branchResolver.UnresolvedTags.StringJoin(", ")));
				}
				commits = branchResolver.Commits;

				var tagResolver = new TagResolver(log, commits, allFiles, switches.TagMatcher);
				if (!tagResolver.ResolveAndFix())
				{
					throw new ImportFailedException(String.Format("Unable to resolve all tags to a single commit: {0}",
							tagResolver.UnresolvedTags.StringJoin(", ")));
				}
				commits = tagResolver.Commits;

				// recheck branches
				if (!branchResolver.Resolve())
				{
					throw new ImportFailedException(String.Format("Resolving tags broke branch resolution: {0}",
							branchResolver.UnresolvedTags.StringJoin(", ")));
				}

				WriteLogFile("allbranches.log", branchResolver.AllTags);
				WriteLogFile("alltags.log", tagResolver.AllTags);

				commits = commits.AssignNumbers();

				var mergeResolver = new MergeResolver(log, commits, branchResolver.ResolvedCommits);
				mergeResolver.Resolve();
				commits = mergeResolver.Commits;
			}
		}

		private static void WriteLogFile(string filename, IEnumerable<string> lines)
		{
			File.WriteAllLines(filename, lines);
		}

		private static void Verify(IEnumerable<Commit> commits)
		{
			foreach (var commit in commits)
			{
				if (!commit.Verify())
				{
					Console.Error.WriteLine("Verification failed: {0} {1}", commit.CommitId, commit.Time);
					foreach (var revision in commit)
						Console.Error.WriteLine("  {0} r{1}", revision.File, revision.Revision);

					bool first = true;
					foreach (var error in commit.Errors)
					{
						if (first)
							first = false;
						else
							Console.Error.WriteLine("----------------------------------------");
						Console.Error.WriteLine(error);
					}

					Console.Error.WriteLine("========================================");
				}
			}
		}
	}
}