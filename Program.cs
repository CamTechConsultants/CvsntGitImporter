/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CvsGitConverter
{
	class Program
	{
		static readonly DateTime StartDate = new DateTime(2003, 2, 1);

		static void Main(string[] args)
		{
			if (args.Length != 1)
				throw new ArgumentException("Need a cvs.log file");

			var parser = new CvsLogParser(args[0], startDate: StartDate);
			var builder = new CommitBuilder(parser);
			var commits = builder.GetCommits().SplitMultiBranchCommits().ToList();

			Verify(commits);

			// build lookup of all files
			var allFiles = new Dictionary<string, FileInfo>();
			foreach (var f in parser.Files)
				allFiles.Add(f.Name, f);

			var tagResolver = new TagResolver(commits, allFiles);
			tagResolver.Resolve();

			if (tagResolver.Errors.Any())
			{
				Console.Error.WriteLine("Errors resolving tags:");
				foreach (var error in tagResolver.Errors)
					Console.Error.WriteLine("  {0}", error);
			}

			WriteLogFile("alltags.log", tagResolver.AllTags);
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