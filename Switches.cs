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
using System.Text;
using System.Text.RegularExpressions;
using CTC.CvsntGitImporter.Utils;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Command line switches.
	/// </summary>
	class Switches : SwitchesDefBase
	{
		[SwitchDef(ShortSwitch="-C", LongSwitch="--config")]
		public ObservableCollection<string> Config { get; set; }

		[SwitchDef(LongSwitch="--debug", ShortSwitch="-d")]
		public bool Debug { get; set; }

		[SwitchDef(LongSwitch="--sandbox")]
		public string Sandbox { get; set; }

		[SwitchDef(LongSwitch="--cvs-cache")]
		public string CvsCache { get; set; }

		[SwitchDef(LongSwitch="--cvs-processes", Description="The number of CVS processes to run in parallel when importing. Defaults to the number of processors on the system.")]
		public string _CvsProcesses { get; set; }


		[SwitchDef(LongSwitch="--default-domain", Description="The default domain name to use for unknown users")]
		public string DefaultDomain { get; set; }

		[SwitchDef(LongSwitch="--user-file", Description="A file specifying user names and e-mail addresses")]
		public string UserFile { get; set; }

		[SwitchDef(LongSwitch="--nobody-name", Description="The name to use for the user when creating tags or manufacturer commits")]
		public string _NobodyName { get; set; }

		[SwitchDef(LongSwitch="--nobody-email", Description="The e-mail address to use for the user when creating tags or manufacturer commits")]
		public string _NobodyEmail { get; set; }


		[SwitchDef(LongSwitch="--exclude")]
		public ObservableCollection<string> _ExcludeFile { get; set; }

		[SwitchDef(LongSwitch="--head-only")]
		public ObservableCollection<string> _HeadOnly { get; set; }

		[SwitchDef(LongSwitch="--head-only-branch")]
		public List<string> HeadOnlyBranches { get; set; }


		[SwitchDef(LongSwitch="--include-tag")]
		public ObservableCollection<string> _IncludeTag { get; set; }

		[SwitchDef(LongSwitch="--exclude-tag")]
		public ObservableCollection<string> _ExcludeTag { get; set; }

		[SwitchDef(LongSwitch="--include-branch")]
		public ObservableCollection<string> _IncludeBranch { get; set; }

		[SwitchDef(LongSwitch="--exclude-branch")]
		public ObservableCollection<string> _ExcludeBranch { get; set; }

		[SwitchDef(LongSwitch="--rename-tag")]
		public ObservableCollection<string> _RenameTag { get; set; }

		[SwitchDef(LongSwitch="--rename-branch")]
		public ObservableCollection<string> _RenameBranch { get; set; }


		/// <summary>
		/// Gets the user to use for creating tags.
		/// </summary>
		public User Nobody { get; private set; }

		/// <summary>
		/// The matcher for files.
		/// </summary>
		public readonly InclusionMatcher FileMatcher = new InclusionMatcher();

		/// <summary>
		/// The matcher for latest-only files.
		/// </summary>
		public readonly InclusionMatcher HeadOnlyMatcher = new InclusionMatcher();

		/// <summary>
		/// The matcher for tags.
		/// </summary>
		public readonly InclusionMatcher TagMatcher = new InclusionMatcher();

		/// <summary>
		/// The matcher for branches.
		/// </summary>
		public readonly InclusionMatcher BranchMatcher = new InclusionMatcher();

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		public readonly Renamer TagRename = new Renamer();

		/// <summary>
		/// The renamer for tags.
		/// </summary>
		public readonly Renamer BranchRename = new Renamer();

		/// <summary>
		/// Gets the number of CVS processes to run.
		/// </summary>
		public int CvsProcesses { get; private set; }


		public Switches()
		{
			Config = new ObservableCollection<string>();
			Config.CollectionChanged += Config_CollectionChanged;

			_ExcludeFile = new RuleCollection(p => AddIncludeRule(FileMatcher, false, p));
			_HeadOnly = new RuleCollection(p => { AddIncludeRule(FileMatcher, false, p); AddIncludeRule(HeadOnlyMatcher, true, p); });
			_IncludeTag = new RuleCollection(p => AddIncludeRule(TagMatcher, true, p));
			_ExcludeTag = new RuleCollection(p => AddIncludeRule(TagMatcher, false, p));
			_IncludeBranch = new RuleCollection(p => AddIncludeRule(BranchMatcher, true, p));
			_ExcludeBranch = new RuleCollection(p => AddIncludeRule(BranchMatcher, false, p));
			_RenameTag = new RuleCollection(r => AddRenameRule(TagRename, r));
			_RenameBranch = new RuleCollection(r => AddRenameRule(BranchRename, r));

			CvsProcesses = Environment.ProcessorCount;
			DefaultDomain = Environment.MachineName;
			_NobodyName = Environment.GetEnvironmentVariable("USERNAME") ?? "nobody";

			BranchRename.AddRule(new Regex("^MAIN$"), "master");
		}

		public override void Verify()
		{
			base.Verify();

			if (this.Sandbox == null)
				throw new CommandLineArgsException("No CVS repository specified");

			if (_CvsProcesses != null)
			{
				int cvsProcesses;
				if (int.TryParse(_CvsProcesses, out cvsProcesses) && cvsProcesses > 0)
					this.CvsProcesses = cvsProcesses;
				else
					throw new CommandLineArgsException("Invalid value for cvs-processes: {0}", _CvsProcesses);
			}
		}

		public override void Parse(params string[] args)
		{
			base.Parse(args);

			var taggerEmail = _NobodyEmail;
			if (taggerEmail == null)
			{
				var name = _NobodyName.Trim();
				var spaceIndex = name.IndexOf(' ');
				if (spaceIndex > 0)
					name = name.Remove(spaceIndex);
				taggerEmail = String.Format("{0}@{1}", name, DefaultDomain);
			}

			this.Nobody = new User(_NobodyName, taggerEmail);
		}

		void ParseConfigFile(string filename)
		{
			try
			{
				var args = ReadConfigFileEntries(filename).ToArray();
				this.Parse(args);
			}
			catch (IOException ioe)
			{
				throw new CommandLineArgsException(String.Format("Unable to open {0}: {1}", filename, ioe.Message), ioe);
			}
			catch (System.Security.SecurityException se)
			{
				throw new CommandLineArgsException(String.Format("Unable to open {0}: {1}", filename, se.Message), se);
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new CommandLineArgsException(String.Format("Unable to open {0}: {1}", filename, uae.Message), uae);
			}
		}

		IEnumerable<string> ReadConfigFileEntries(string filename)
		{
			int lineNo = 0;
			foreach (var rawLine in File.ReadLines(filename, Encoding.UTF8))
			{
				lineNo++;
				var line = Regex.Replace(rawLine, @"#.*$", "").Trim();
				if (Regex.IsMatch(line, @"^\s*$"))
					continue;

				var match = Regex.Match(line, @"^(\S+)\s+(.*)");
				if (match.Success)
				{
					// switch with an argument
					yield return String.Format("--{0}", match.Groups[1].Value);
					yield return match.Groups[2].Value.Trim();
				}
				else if (!Regex.IsMatch(line, @"\s"))
				{
					// boolean switch
					yield return String.Format("--{0}", line);
				}
				else
				{
					throw new CommandLineArgsException("{0}({1}): unrecognised input", filename, lineNo);
				}
			}
		}

		void Config_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Parse config files as they're encountered
			if (e.Action == NotifyCollectionChangedAction.Add)
				ParseConfigFile(e.NewItems[0] as string);
		}

		private void AddIncludeRule(InclusionMatcher matcher, bool include, string pattern)
		{
			try
			{
				var regex = new Regex(pattern);

				if (include)
					matcher.AddIncludeRule(regex);
				else
					matcher.AddExcludeRule(regex);
			}
			catch (ArgumentException)
			{
				throw new CommandLineArgsException("Invalid regex: {0}", pattern);
			}
		}

		private void AddRenameRule(Renamer renamer, string rule)
		{
			var parts = rule.Split('/');
			if (parts.Length != 2)
				throw new CommandLineArgsException("Invalid rename rule: {0}", rule);

			try
			{
				var regex = new Regex(parts[0].Trim());
				renamer.AddRule(regex, parts[1].Trim());
			}
			catch (ArgumentException)
			{
				throw new CommandLineArgsException("Invalid regex: {0}", parts[0]);
			}
		}

		private class RuleCollection : ObservableCollection<string>
		{
			private readonly Action<string> m_action;

			public RuleCollection(Action<string> action)
			{
				m_action = action;
			}

			protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
			{
				base.OnCollectionChanged(e);
				m_action(e.NewItems[0] as string);
			}
		}
	}
}