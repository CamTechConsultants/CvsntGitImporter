/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CTC.CvsntGitImporter.Utils;

namespace CTC.CvsntGitImporter
{
	class Config : IConfig
	{
		private readonly Switches m_switches;
		private readonly string m_debugLogDir;
		private User m_nobody;
		private UserMap m_userMap;
		private List<GitConfigOption> m_gitConfigOptions;
		private readonly InclusionMatcher m_fileMatcher = new InclusionMatcher(ignoreCase: true);
		private readonly InclusionMatcher m_headOnlyMatcher = new InclusionMatcher(ignoreCase: true) { Default = false };


		public Config(Switches switches)
		{
			m_switches = switches;
			m_debugLogDir = Path.Combine(Environment.CurrentDirectory, "DebugLogs");

			ObserveCollection(m_switches.GitConfigSet, x => AddGitConfigOption(x, add: false));
			ObserveCollection(m_switches.GitConfigAdd, x => AddGitConfigOption(x, add: true));

			TagMatcher = new InclusionMatcher(ignoreCase: false);
			TagRename = new Renamer();

			BranchMatcher = new InclusionMatcher(ignoreCase: false);
			BranchRename = new Renamer();
			BranchRename.AddRule(new RenameRule("^MAIN$", "master"));

			ObserveCollection(m_switches.IncludeFile, x => AddIncludeRule(m_fileMatcher, x, include: true));
			ObserveCollection(m_switches.ExcludeFile, x => AddIncludeRule(m_fileMatcher, x, include: false));
			ObserveCollection(m_switches.HeadOnly, x => AddIncludeRule(m_headOnlyMatcher, x, include:true));

			ObserveCollection(m_switches.IncludeTag, x => AddIncludeRule(TagMatcher, x, include: true));
			ObserveCollection(m_switches.ExcludeTag, x => AddIncludeRule(TagMatcher, x, include: false));
			ObserveCollection(m_switches.RenameTag, x => AddRenameRule(TagRename, x));

			ObserveCollection(m_switches.IncludeBranch, x => AddIncludeRule(BranchMatcher, x, include: true));
			ObserveCollection(m_switches.ExcludeBranch, x => AddIncludeRule(BranchMatcher, x, include: false));
			ObserveCollection(m_switches.RenameBranch, x => AddRenameRule(BranchRename, x));
		}

		public void ParseCommandLineSwitches(params string[] args)
		{
			m_switches.Parse(args);

			try
			{
				if (m_switches.BranchpointRule != null)
					BranchpointRule = RenameRule.Parse(m_switches.BranchpointRule);
			}
			catch (ArgumentException ae)
			{
				throw new CommandLineArgsException("Invalid branchpoint rule: {0}", ae.Message);
			}
		}


		#region General config

		/// <summary>
		/// Is debug output and logging enabled?
		/// </summary>
		public bool Debug
		{
			get { return m_switches.Debug; }
		}

		/// <summary>
		/// The directory in which debug logs are stored. Never null.
		/// </summary>
		public string DebugLogDir
		{
			get { return m_debugLogDir; }
		}

		/// <summary>
		/// Should we actually import the data?
		/// </summary>
		public bool DoImport
		{
			get { return !m_switches.NoImport; }
		}

		/// <summary>
		/// Do we need to create the CVS log file?
		/// </summary>
		public bool CreateCvsLog
		{
			get { return m_switches.CvsLog == null || !File.Exists(m_switches.CvsLog); }
		}

		/// <summary>
		/// The name of the CVS log file. Never null.
		/// </summary>
		public string CvsLogFileName
		{
			get { return m_switches.CvsLog ?? Path.Combine(DebugLogDir, "cvs.log"); }
		}

		/// <summary>
		/// The path to the CVS sandbox. Not null.
		/// </summary>
		public string Sandbox
		{
			get { return m_switches.Sandbox; }
		}

		/// <summary>
		/// The path to the Git repository to create. Not null.
		/// </summary>
		public string GitDir
		{
			get { return m_switches.GitDir; }
		}

		/// <summary>
		/// Gets any configuration options to apply to the new repository.
		/// </summary>
		public IEnumerable<GitConfigOption> GitConfig
		{
			get { return m_gitConfigOptions ?? Enumerable.Empty<GitConfigOption>(); }
		}

		/// <summary>
		/// Should we repack the git repository after import?
		/// </summary>
		public bool Repack
		{
			get { return m_switches.Repack; }
		}

		/// <summary>
		/// The path to the CVS cache, if specified, otherwise null.
		/// </summary>
		public string CvsCache
		{
			get { return m_switches.CvsCache; }
		}

		/// <summary>
		/// Gets the number of CVS processes to run.
		/// </summary>
		public uint CvsProcesses
		{
			get { return m_switches.CvsProcesses ?? (uint)Environment.ProcessorCount; }
		}

		#endregion General config


		#region Users

		/// <summary>
		/// The default domain for user e-mail addresses. Not null.
		/// </summary>
		public string DefaultDomain
		{
			get { return m_switches.DefaultDomain ?? Environment.MachineName; }
		}

		/// <summary>
		/// A file containing user mappings, if provided, otherwise null.
		/// </summary>
		public UserMap Users
		{
			get { return m_userMap ?? (m_userMap = GetUserMap()); }
		}

		/// <summary>
		/// Gets the user to use for creating tags. Never null.
		/// </summary>
		public User Nobody
		{
			get { return m_nobody ?? (m_nobody = GetNobodyUser()); }
		}
		
		private User GetNobodyUser()
		{
			var taggerEmail = m_switches.NobodyEmail;
			if (taggerEmail == null)
			{
				var name = m_switches.NobodyName ?? Environment.GetEnvironmentVariable("USERNAME") ?? "nobody";
				name = name.Trim();

				var spaceIndex = name.IndexOf(' ');
				if (spaceIndex > 0)
					name = name.Remove(spaceIndex);
				taggerEmail = String.Format("{0}@{1}", name, DefaultDomain);
			}

			return new User(m_switches.NobodyName, taggerEmail);
		}

		private UserMap GetUserMap()
		{
			var m_userMap = new UserMap(this.DefaultDomain);
			m_userMap.AddEntry("", this.Nobody);

			if (m_switches.UserFile != null)
				m_userMap.ParseUserFile(m_switches.UserFile);

			return m_userMap;
		}

		#endregion Users


		#region File inclusion

		/// <summary>
		/// The branches to import "head-only" files for.
		/// </summary>
		public IEnumerable<string> HeadOnlyBranches
		{
			get { return m_switches.HeadOnlyBranches ?? Enumerable.Empty<string>(); }
		}

		/// <summary>
		/// Should a file be imported?
		/// </summary>
		/// <remarks>Excludes files that are "head-only"</remarks>
		public bool IncludeFile(string filename)
		{
			return m_fileMatcher.Match(filename) && !m_headOnlyMatcher.Match(filename);
		}

		/// <summary>
		/// Is a file a "head-only" file, i.e. one whose head revision only should be imported?
		/// </summary>
		public bool IsHeadOnly(string filename)
		{
			return m_fileMatcher.Match(filename) && m_headOnlyMatcher.Match(filename);
		}

		#endregion


		#region Tags

		/// <summary>
		/// The default value for PartialTagThreshold.
		/// </summary>
		public const int DefaultPartialTagThreshold = 30;

		/// <summary>
		/// The number of missing files before we declare a tag to be "partial".
		/// </summary>
		public int PartialTagThreshold
		{
			get { return (int)m_switches.PartialTagThreshold.GetValueOrDefault(DefaultPartialTagThreshold); }
		}

		/// <summary>
		/// The matcher for tags.
		/// </summary>
		public InclusionMatcher TagMatcher { get; private set; }

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		public Renamer TagRename { get; private set; }

		/// <summary>
		/// The tag to mark imports with.
		/// </summary>
		public string MarkerTag
		{
			get
			{
				if (m_switches.MarkerTag == null)
					return "cvs-import";
				else if (m_switches.MarkerTag.Length == 0)
					return null;
				else
					return m_switches.MarkerTag;
			}
		}

		#endregion Tags


		#region Branches

		/// <summary>
		/// A rule to translate branch names into branchpoint tag names if specified, otherwise null.
		/// </summary>
		public RenameRule BranchpointRule { get; private set; }

		/// <summary>
		/// The matcher for branches.
		/// </summary>
		public InclusionMatcher BranchMatcher { get; private set; }

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		public Renamer BranchRename { get; private set; }

		#endregion Branches


		private void AddGitConfigOption(string x, bool add)
		{
			try
			{
				var option = GitConfigOption.Parse(x, add);

				if (m_gitConfigOptions == null)
					m_gitConfigOptions = new List<GitConfigOption>() { option };
				else
					m_gitConfigOptions.Add(option);
			}
			catch (ArgumentException ae)
			{
				throw new CommandLineArgsException("Invalid git option: {0}", ae.Message);
			}
		}

		private void AddIncludeRule(InclusionMatcher matcher, string pattern, bool include)
		{
			try
			{
				if (include)
					matcher.AddIncludeRule(pattern);
				else
					matcher.AddExcludeRule(pattern);
			}
			catch (ArgumentException)
			{
				throw new CommandLineArgsException("Invalid regex: {0}", pattern);
			}
		}

		private void AddRenameRule(Renamer renamer, string rule)
		{
			try
			{
				renamer.AddRule(RenameRule.Parse(rule));
			}
			catch (ArgumentException ae)
			{
				throw new CommandLineArgsException("Invalid rename rule: {0}", ae.Message);
			}
		}

		private void ObserveCollection(ObservableCollection<string> collection, Action<string> handler)
		{
			collection.CollectionChanged += (_, e) => handler(e.NewItems[0] as string);
		}
	}
}