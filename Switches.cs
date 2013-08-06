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
using CTC.CvsntGitImporter.Properties;
using CTC.CvsntGitImporter.Utils;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Command line switches.
	/// </summary>
	class Switches : SwitchesDefBase
	{
		[SwitchDef(ShortSwitch="-C", LongSwitch="--config", ValueDescription="filename", Description="Specify a configuration file containing parameters")]
		public ObservableCollection<string> Config { get; set; }

		[SwitchDef(LongSwitch="--debug", ShortSwitch="-d", Description="Enable extra debug log files")]
		public bool Debug { get; set; }

		[SwitchDef(LongSwitch="--help", ShortSwitch="-h", Description="Display this help")]
		public bool Help { get; set; }

		[SwitchDef(LongSwitch="--cvs-log", Description="The CVS log file to use for metadata")]
		public string CvsLog { get; set; }

		[SwitchDef(LongSwitch="--sandbox", Description="The location of the checked out source code from CVS. Required")]
		public string Sandbox { get; set; }

		[SwitchDef(LongSwitch="--noimport", ShortSwitch="-n", Description="Don't actually import the data, just do the analysis")]
		public bool NoImport { get; set; }

		[SwitchDef(LongSwitch="--gitdir", ValueDescription="dir", Description="The directory to create the git repository in. Must not exist or be empty")]
		public string GitDir { get; set; }

		[SwitchDef(LongSwitch="--git-config-set", ValueDescription="name=value", Description="Sets a git option in the repository. The argument is in the form name=value")]
		public ObservableCollection<string> GitConfigSet { get; set; }

		[SwitchDef(LongSwitch="--git-config-add", ValueDescription="name=value", Description="Adds a git option in the repository. The argument is in the form name=value")]
		public ObservableCollection<string> GitConfigAdd { get; set; }

		[SwitchDef(LongSwitch="--repack", Description="Repack the repository after the import")]
		public bool Repack { get; set; }

		[SwitchDef(LongSwitch="--cvs-cache", Description="A directory to cache versions of files in. Useful if the import needs to be run more than once")]
		public string CvsCache { get; set; }

		[SwitchDef(LongSwitch="--cvs-processes", Description="The number of CVS processes to run in parallel when importing. Defaults to the number of processors on the system")]
		public uint? CvsProcesses { get; set; }

		[SwitchDef(LongSwitch = "--partial-tag-threshold", Description = "The number of untagged files encountered before a tag is declared to be a partial tag. Set to zero to disable partial tag detection")]
		public uint? PartialTagThreshold { get; set; }

		[SwitchDef(LongSwitch = "--import-marker-tag", Description = "The tag to mark the import with")]
		public string MarkerTag { get; set; }


		[SwitchDef(LongSwitch="--default-domain", Description="The default domain name to use for unknown users")]
		public string DefaultDomain { get; set; }

		[SwitchDef(LongSwitch="--user-file", Description="A file specifying user names and e-mail addresses")]
		public string UserFile { get; set; }

		[SwitchDef(LongSwitch="--nobody-name", Description="The name to use for the user when creating tags or manufacturer commits")]
		public string NobodyName { get; set; }

		[SwitchDef(LongSwitch="--nobody-email", Description="The e-mail address to use for the user when creating tags or manufacturer commits")]
		public string NobodyEmail { get; set; }


		[SwitchDef(LongSwitch="--include", ValueDescription="regex", Description="A pattern to match files to include")]
		public ObservableCollection<string> IncludeFile { get; set; }

		[SwitchDef(LongSwitch="--exclude", ValueDescription="regex", Description="A pattern to match files to exclude")]
		public ObservableCollection<string> ExcludeFile { get; set; }

		[SwitchDef(LongSwitch="--head-only", ValueDescription="regex", Description="A pattern to match files that should have just their head version imported and no historical versions")]
		public ObservableCollection<string> HeadOnly { get; set; }

		[SwitchDef(LongSwitch="--head-only-branch", ValueDescription="name", Description="A branch that should have the head version imported for all files that match the --head-only patterns")]
		public List<string> HeadOnlyBranches { get; set; }


		[SwitchDef(LongSwitch="--branchpoint-rule", ValueDescription="rule", Description="A rule to obtain a branchpoint tag for a branch")]
		public string BranchpointRule { get; set; }

		[SwitchDef(LongSwitch="--include-tag", ValueDescription="regex", Description="A pattern to match tags that should be imported")]
		public ObservableCollection<string> IncludeTag { get; set; }

		[SwitchDef(LongSwitch="--exclude-tag", ValueDescription="regex", Description="A pattern to match tags that should not be imported")]
		public ObservableCollection<string> ExcludeTag { get; set; }

		[SwitchDef(LongSwitch="--include-branch", ValueDescription="regex", Description="A pattern to match branches that should be imported")]
		public ObservableCollection<string> IncludeBranch { get; set; }

		[SwitchDef(LongSwitch="--exclude-branch", ValueDescription="regex", Description="A pattern to match branches that should not be imported")]
		public ObservableCollection<string> ExcludeBranch { get; set; }

		[SwitchDef(LongSwitch="--rename-tag", ValueDescription="rule", Description="A rule to rename tags as they're imported")]
		public ObservableCollection<string> RenameTag { get; set; }

		[SwitchDef(LongSwitch="--rename-branch", ValueDescription="rule", Description="to rename branches as they're imported")]
		public ObservableCollection<string> RenameBranch { get; set; }


		public Switches()
		{
			Config = new ObservableCollection<string>();
			Config.CollectionChanged += Config_CollectionChanged;

			GitConfigSet = new ObservableCollection<string>();
			GitConfigAdd = new ObservableCollection<string>();

			IncludeFile = new ObservableCollection<string>();
			ExcludeFile = new ObservableCollection<string>();
			HeadOnly = new ObservableCollection<string>();

			IncludeTag = new ObservableCollection<string>();
			ExcludeTag = new ObservableCollection<string>();
			RenameTag = new ObservableCollection<string>();

			IncludeBranch = new ObservableCollection<string>();
			ExcludeBranch = new ObservableCollection<string>();
			RenameBranch = new ObservableCollection<string>();
		}

		public override void Verify()
		{
			base.Verify();

			if (this.Help)
				return;

			if (this.Sandbox == null)
				throw new CommandLineArgsException("No CVS repository specified");
			else if (!Directory.Exists(this.Sandbox))
				throw new CommandLineArgsException("Sandbox directory {0} does not exist", this.Sandbox);

			if (CvsProcesses == 0 || CvsProcesses > Cvs.MaxProcessCount)
				throw new CommandLineArgsException("Invalid number of CVS processes: {0}", CvsProcesses);

			if (GitDir != null && !NoImport && Directory.Exists(GitDir))
			{
				if (Directory.EnumerateFileSystemEntries(GitDir).Any())
					throw new CommandLineArgsException("Git directory {0} is not empty", GitDir);
			}
		}

		public override string GetHelpText(int maxWidth)
		{
			var buf = new StringBuilder(base.GetHelpText(maxWidth));

			buf.AppendLine();
			foreach (var i in Wrap(Resources.ExtraHelp, maxWidth))
				buf.AppendLine(i);

			return buf.ToString();
		}

		void ParseConfigFile(string filename)
		{
			try
			{
				var args = ReadConfigFileEntries(filename).ToArray();
				SwitchesParser.Parse(this, args);
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
					yield return Unquote(match.Groups[2].Value.Trim());
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

		static string Unquote(string value)
		{
			if (value == null)
				return null;

			var match = Regex.Match(value, @"^\s*""(.*)""\s*$");
			return match.Success ? match.Groups[1].Value : value;
		}
	}
}