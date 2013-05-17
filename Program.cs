/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CvsGitConverter.Utils;

namespace CvsGitConverter
{
	class Program
	{
		private static readonly Switches m_switches = new Switches();
		private static readonly string m_logDir = Path.Combine(Environment.CurrentDirectory, "gitconvert");

		static int Main(string[] args)
		{
			try
			{
				m_switches.Parse(args);

				if (m_switches.ExtraArguments.Count != 1)
					throw new ArgumentException("Need a cvs.log file");

				Directory.CreateDirectory(m_logDir);
				using (var log = new Logger(GetLogFilePath("import.log")))
				{
					Import(log);
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				return 1;
			}

			return 0;
		}

		private static void Import(Logger log)
		{
			var parser = new CvsLogParser(m_switches.Sandbox, m_switches.ExtraArguments[0]);
			var builder = new CommitBuilder(parser.Parse());
			IEnumerable<Commit> commits = builder.GetCommits()
					.SplitMultiBranchCommits()
					.AddCommitsToFiles()
					.ToListIfNeeded();

			Verify(commits);

			// build lookup of all files
			var allFiles = new Dictionary<string, FileInfo>();
			foreach (var f in parser.Files)
				allFiles.Add(f.Name, f);

			// resolve branchpoints
			var branchResolver = new BranchResolver(log, commits, allFiles, m_switches.BranchMatcher);
			if (!branchResolver.ResolveAndFix())
			{
				throw new ImportFailedException(String.Format("Unable to resolve all branches to a single commit: {0}",
						branchResolver.UnresolvedTags.StringJoin(", ")));
			}
			commits = branchResolver.Commits;

			// resolve tags
			var tagResolver = new TagResolver(log, commits, allFiles, m_switches.TagMatcher);
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

			var streams = commits.SplitBranchStreams(branchResolver.ResolvedCommits);

			// resolve merges
			var mergeResolver = new MergeResolver(log, streams, branchResolver.AllTags);
			mergeResolver.Resolve();

			WriteBranchLogs(streams);

			// do the import
			ICvsRepository repository = new CvsRepository(m_switches.Sandbox);
			if (m_switches.CvsCache != null)
				repository = new CvsRepositoryCache(m_switches.CvsCache, repository);

			var cvs = new Cvs(repository, m_switches.CvsProcesses);
			var importer = new Importer(log, streams, cvs);
			importer.Import();
		}

		private static void WriteBranchLogs(BranchStreamCollection streams)
		{
			foreach (var branch in streams.Branches)
			{
				var filename = String.Format("commits-{0}.log", branch);
				using (var writer = new StreamWriter(GetLogFilePath(filename), append: false, encoding: Encoding.UTF8))
				{
					writer.WriteLine("Branch: {0}", branch);
					writer.WriteLine();

					for (var c = streams[branch]; c != null; c = c.Successor)
						WriteCommitLog(writer, c);
				}
			}
		}

		private static void WriteCommitLog(StreamWriter writer, Commit c)
		{
			writer.WriteLine("Commit {0}/{1}", c.CommitId, c.Index);
			writer.WriteLine("{0} by {1}", c.Time, c.Author);

			foreach (var branchCommit in c.Branches)
				writer.WriteLine("Branchpoint for {0}", branchCommit.Branch);

			if (c.MergeFrom != null)
				writer.WriteLine("Merge from {0}/{1} on {2}", c.MergeFrom.CommitId, c.MergeFrom.Index, c.MergeFrom.Branch);

			foreach (var revision in c.OrderBy(r => r.File.Name, StringComparer.OrdinalIgnoreCase))
			{
				if (revision.IsDead)
				writer.Write("  {0} deleted", revision.File.Name);
				else
				writer.Write("  {0} r{1}", revision.File.Name, revision.Revision);

				if (revision.Mergepoint != Revision.Empty)
					writer.WriteLine(" merge from {0} on {1}", revision.Mergepoint, revision.File.GetBranch(revision.Mergepoint));
				else
					writer.WriteLine();
			}

			writer.WriteLine();
		}

		private static void WriteLogFile(string filename, IEnumerable<string> lines)
		{
			if (m_switches.Debug)
			{
				var logPath = GetLogFilePath(filename);
				File.WriteAllLines(logPath, lines);
			}
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

		private static string GetLogFilePath(string filename)
		{
			return Path.Combine(m_logDir, filename);
		}
	}
}