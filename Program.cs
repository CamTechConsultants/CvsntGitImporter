/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CTC.CvsntGitImporter.Utils;

namespace CTC.CvsntGitImporter
{
	class Program
	{
		private static readonly Switches m_switches = new Switches();
		private static Logger m_log;
		private static UserMap m_userMap;
		private static BranchStreamCollection m_streams;
		private static IDictionary<string, Commit> m_resolvedTags;

		static int Main(string[] args)
		{
			try
			{
				m_switches.Parse(args);

				if (m_switches.Help)
				{
					Console.Out.WriteLine(m_switches.GetHelpText());
					return 0;
				}

				// parse user file
				m_userMap = new UserMap(m_switches.DefaultDomain);
				m_userMap.AddEntry("", m_switches.Nobody);
				if (m_switches.UserFile != null)
					m_userMap.ParseUserFile(m_switches.UserFile);

				var logDir = Path.Combine(Environment.CurrentDirectory, "DebugLogs");
				Directory.CreateDirectory(logDir);
				using (m_log = new Logger(logDir, debugEnabled: m_switches.Debug))
				{
					var cvsLogFile = m_switches.CvsLog;
					if (m_switches.CvsLog == null || !File.Exists(m_switches.CvsLog))
					{
						if (cvsLogFile == null)
							cvsLogFile = Path.Combine(logDir, "cvs.log");
						RunOperation("Download CVS Log", () => GetCvsLog(cvsLogFile, m_switches.Sandbox));
					}

					RunOperation("Analysis", () => Analyse(cvsLogFile));

					if (m_switches.DoImport)
						RunOperation("Import", Import);
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				return 1;
			}

			return 0;
		}

		private static void RunOperation(string name, Action operation)
		{
			m_log.DoubleRuleOff();
			m_log.WriteLine("{0} started at {1}", name, DateTime.Now);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			try
			{
				operation();
			}
			finally
			{
				stopwatch.Stop();
				m_log.WriteLine("{0} took {1}", name, stopwatch.Elapsed);
			}
		}

		private static void GetCvsLog(string cvsLogFile, string sandbox)
		{
			var module = ReadModuleName(sandbox);

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "cmd.exe",
					Arguments = String.Format("/C cvs rlog \"{0}\" > \"{1}\"", module, cvsLogFile),
					UseShellExecute = false,
					RedirectStandardError = true,
					StandardErrorEncoding = Encoding.Default,
					CreateNoWindow = true,
				},
			};

			var error = new StringBuilder();
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					error.Append(e.Data);
			};

			process.Start();

			process.BeginErrorReadLine();
			process.WaitForExit();

			if (error.Length > 0)
				throw new ImportFailedException(String.Format("Failed to get CVS log: {0}", error));
			else if (process.ExitCode != 0)
				throw new ImportFailedException(String.Format("Failed to get CVS log: cvs exited with exit code {0}", process.ExitCode));
		}

		private static string ReadModuleName(string sandbox)
		{
			var repoPath = Path.Combine(sandbox, @"CVS\Repository");
			return File.ReadAllText(repoPath).Trim();
		}

		private static void Analyse(string cvsLogFile)
		{
			var parser = new CvsLogParser(m_switches.Sandbox, cvsLogFile, m_switches.BranchMatcher);
			var builder = new CommitBuilder(m_log, parser.Parse());
			var exclusionFilter = new ExclusionFilter(m_log, m_switches.FileMatcher, m_switches.HeadOnlyMatcher, m_switches.BranchRename);

			IEnumerable<Commit> commits = builder.GetCommits()
					.SplitMultiBranchCommits()
					.FilterCommitsOnExcludedBranches()
					.FilterExcludedFiles(exclusionFilter)
					.AddCommitsToFiles()
					.Verify(m_log)
					.ToListIfNeeded();

			// build lookup of all files
			var allFiles = new FileCollection(parser.Files);
			var includedFiles = new FileCollection(parser.Files.Where(f => m_switches.FileMatcher.Match(f.Name)));

			WriteAllCommitsLog(commits);
			WriteExcludedFileLog(parser);

			var branchResolver = ResolveBranches(commits, includedFiles);
			commits = branchResolver.Commits;

			var tagResolver = ResolveTags(commits, includedFiles);
			commits = tagResolver.Commits;

			WriteTagLog("allbranches.log", branchResolver, parser.ExcludedBranches, m_switches.BranchRename);
			WriteTagLog("alltags.log", tagResolver, parser.ExcludedTags, m_switches.TagRename);
			WriteUserLog("allusers.log", commits);

			var streams = commits.SplitBranchStreams(branchResolver.ResolvedTags);

			// resolve merges
			var mergeResolver = new MergeResolver(m_log, streams);
			mergeResolver.Resolve();

			WriteBranchLogs(streams);

			// add any "head-only" files
			exclusionFilter.CreateHeadOnlyCommits(m_switches.HeadOnlyBranches, streams, allFiles);

			// store data needed for import
			m_streams = streams;
		}

		private static ITagResolver ResolveBranches(IEnumerable<Commit> commits, FileCollection includedFiles)
		{
			ITagResolver branchResolver;
			var autoBranchResolver = new AutoBranchResolver(m_log, includedFiles);
			if (m_switches.PartialTagThreshold != null)
				autoBranchResolver.PartialTagThreshold = (int)m_switches.PartialTagThreshold.Value;
			branchResolver = autoBranchResolver;

			// if we're matching branchpoints, resolve those tags first
			if (m_switches.BranchpointRule != null)
			{
				var tagResolver = new TagResolver(m_log, includedFiles);
				if (m_switches.PartialTagThreshold != null)
					tagResolver.PartialTagThreshold = (int)m_switches.PartialTagThreshold.Value;

				var allBranches = includedFiles.SelectMany(f => f.AllBranches).Distinct();
				var rule = m_switches.BranchpointRule;
				var branchpointTags = allBranches.Where(b => rule.IsMatch(b)).Select(b => rule.Apply(b));

				if (!tagResolver.Resolve(branchpointTags, commits))
				{
					var unresolvedTags = tagResolver.UnresolvedTags.OrderBy(i => i);
					m_log.WriteLine("Unresolved branchpoint tags:");

					using (m_log.Indent())
					{
						foreach (var tag in unresolvedTags)
							m_log.WriteLine("{0}", tag);
					}
				}

				commits = tagResolver.Commits;
				branchResolver = new ManualBranchResolver(m_log, autoBranchResolver, tagResolver, m_switches.BranchpointRule);
			}

			// resolve remaining branchpoints 
			if (!branchResolver.Resolve(includedFiles.SelectMany(f => f.AllBranches).Distinct(), commits))
			{
				var unresolvedTags = branchResolver.UnresolvedTags.OrderBy(i => i);
				m_log.WriteLine("Unresolved branches:");

				using (m_log.Indent())
				{
					foreach (var tag in unresolvedTags)
						m_log.WriteLine("{0}", tag);
				}

				throw new ImportFailedException(String.Format("Unable to resolve all branches to a single commit: {0}",
						branchResolver.UnresolvedTags.StringJoin(", ")));
			}

			return branchResolver;
		}

		private static ITagResolver ResolveTags(IEnumerable<Commit> commits, FileCollection includedFiles)
		{
			var tagResolver = new TagResolver(m_log, includedFiles);
			if (m_switches.PartialTagThreshold != null)
				tagResolver.PartialTagThreshold = (int)m_switches.PartialTagThreshold.Value;

			// resolve tags
			var allTags = includedFiles.SelectMany(f => f.AllTags).Where(t => m_switches.TagMatcher.Match(t));
			if (!tagResolver.Resolve(allTags.Distinct(), commits))
			{
				// ignore branchpoint tags that are unresolved
				var unresolvedTags = tagResolver.UnresolvedTags.OrderBy(i => i);
				m_log.WriteLine("Unresolved tags:");

				using (m_log.Indent())
				{
					foreach (var tag in unresolvedTags)
						m_log.WriteLine("{0}", tag);
				}

				throw new ImportFailedException(String.Format("Unable to resolve all tags to a single commit: {0}",
						unresolvedTags.StringJoin(", ")));
			}

			m_resolvedTags = tagResolver.ResolvedTags;
			return tagResolver;
		}

		private static void Import()
		{
			// do the import
			ICvsRepository repository = new CvsRepository(m_switches.Sandbox);
			if (m_switches.CvsCache != null)
				repository = new CvsRepositoryCache(m_switches.CvsCache, repository);

			var cvs = new Cvs(repository, m_switches.CvsProcesses);
			var importer = new Importer(m_log, m_switches, m_userMap, m_streams, m_resolvedTags, cvs);
			importer.Import();
		}

		private static void WriteExcludedFileLog(CvsLogParser parser)
		{
			if (m_log.DebugEnabled)
			{
				var files = parser.Files
						.Select(f => f.Name)
						.Where(f => !m_switches.FileMatcher.Match(f) && !m_switches.HeadOnlyMatcher.Match(f))
						.OrderBy(i => i, StringComparer.OrdinalIgnoreCase);

				m_log.WriteDebugFile("excluded_files.log", files);

				var headOnly = parser.Files
						.Select(f => f.Name)
						.Where(f => m_switches.HeadOnlyMatcher.Match(f))
						.OrderBy(i => i, StringComparer.OrdinalIgnoreCase);

				m_log.WriteDebugFile("headonly_files.log", headOnly);
			}
		}

		private static void WriteAllCommitsLog(IEnumerable<Commit> commits)
		{
			if (!m_log.DebugEnabled)
				return;

			using (var log = m_log.OpenDebugFile("allcommits.log"))
			{
				foreach (var commit in commits)
				{
					log.WriteLine("Commit {0}", commit.CommitId);
					log.WriteLine(commit.Message);
					log.WriteLine("{0} | {1} | {2}", commit.Branch, commit.Author, commit.Time);

					foreach (var r in commit)
						log.WriteLine("  {0} r{1}", r.File.Name, r.Revision);
					log.WriteLine();
				}
			}
		}

		private static void WriteTagLog(string filename, ITagResolver resolver, IEnumerable<string> excluded, Renamer renamer)
		{
			if (m_log.DebugEnabled)
			{
				using (var log = m_log.OpenDebugFile(filename))
				{
					if (resolver.ResolvedTags().Any())
					{
						var included = resolver.ResolvedTags()
								.Select(t => "  " + PrintPossibleRename(t, renamer))
								.ToList();

						log.WriteLine("Included:");
						log.Write(String.Join(Environment.NewLine, included));
						log.WriteLine();
						log.WriteLine();
					}

					if (excluded.Any())
					{
						var excludedDisplay = excluded
								.Select(t => "  " + PrintPossibleRename(t, renamer))
								.ToList();

						log.WriteLine("Excluded:");
						log.Write(String.Join(Environment.NewLine, excludedDisplay));
						log.WriteLine();
					}
				}
			}
		}

		private static void WriteBranchLogs(BranchStreamCollection streams)
		{
			foreach (var branch in streams.Branches)
			{
				var filename = String.Format("commits-{0}.log", branch);
				using (var writer = m_log.OpenDebugFile(filename))
				{
					writer.WriteLine("Branch: {0}", branch);
					writer.WriteLine();

					for (var c = streams[branch]; c != null; c = c.Successor)
						WriteCommitLog(writer, c);
				}
			}
		}

		private static void WriteUserLog(string filename, IEnumerable<Commit> commits)
		{
			if (!m_log.DebugEnabled)
				return;

			var allUsers = commits.Select(c => c.Author)
				.Distinct()
				.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
				.Select(name =>
				{
					var user = m_userMap.GetUser(name);
					if (user.Generated)
						return name;
					else
						return String.Format("{0} ({1})", name, user);
				});

			m_log.WriteDebugFile(filename, allUsers);
		}

		private static string PrintPossibleRename(string tag, Renamer renamer)
		{
			var renamed = renamer.Process(tag);
			if (renamed == tag)
				return tag;
			else
				return String.Format("{0} (renamed to {1})", tag, renamed);
		}

		private static void WriteCommitLog(TextWriter writer, Commit c)
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
	}
}