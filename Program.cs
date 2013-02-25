/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
				throw new ArgumentException("Need a cvs.log file");

			var parser = new CvsLogParser(args[0]);
			var revisions = from r in parser.Parse()
							where !(r.Revision == "1.1" && Regex.IsMatch(r.Message, @"file .* was initially added on branch "))
							select r;

			var commits = new Dictionary<string, Commit>();

			foreach (var revision in revisions)
			{
				Commit commit;
				if (commits.TryGetValue(revision.CommitId, out commit))
				{
					commit.Add(revision);
				}
				else
				{
					commit = new Commit(revision.CommitId) { revision };
					commits.Add(commit.CommitId, commit);
				}
			}

			foreach (var commit in commits.Values.OrderBy(c => c.Time))
			{
				if (!commit.Verify())
				{
					Console.Error.WriteLine("Verification failed: {0} {1}", commit.CommitId, commit.Time);
					foreach (var revision in commit)
						Console.Error.WriteLine("  {0} r{1}", revision.File, revision.Revision);

					foreach (var error in commit.Errors)
					{
						Console.Error.WriteLine(error);
						Console.Error.WriteLine("========================================");
					}
				}
			}

			foreach (var f in parser.Files)
			{
				Console.Out.WriteLine("File {0}", f.Name);
				foreach (var t in f.Branches)
					Console.Out.WriteLine("  {0} = {1}", t.Key, t.Value);
			}
		}
	}
}