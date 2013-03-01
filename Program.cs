/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;

namespace CvsGitConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
				throw new ArgumentException("Need a cvs.log file");

			var parser = new CvsLogParser(args[0]);
			var builder = new CommitBuilder(parser);
			var commits = builder.GetCommits();

			Verify(commits);
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