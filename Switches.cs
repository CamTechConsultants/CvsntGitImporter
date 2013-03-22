/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CvsGitImporter.Utils;

namespace CvsGitConverter
{
	/// <summary>
	/// Command line switches.
	/// </summary>
	class Switches : SwitchesDefBase
	{
		[SwitchDef(ShortSwitch="-C", LongSwitch="--config")]
		public ObservableCollection<string> Config { get; set; }

		[SwitchDef(LongSwitch="--include-tag")]
		public ObservableCollection<string> IncludeTag { get; set; }

		[SwitchDef(LongSwitch="--exclude-tag")]
		public ObservableCollection<string> ExcludeTag { get; set; }

		public readonly InclusionMatcher TagMatcher = new InclusionMatcher();

		public Switches()
		{
			Config = new ObservableCollection<string>();
			Config.CollectionChanged += Config_CollectionChanged;

			IncludeTag = new ObservableCollection<string>();
			IncludeTag.CollectionChanged += IncludeTag_CollectionChanged;

			ExcludeTag = new ObservableCollection<string>();
			ExcludeTag.CollectionChanged += ExcludeTag_CollectionChanged;
		}


		void AddTagRule(string pattern, bool include)
		{
			Regex regex;
			try
			{
				regex = new Regex(pattern);
			}
			catch (ArgumentException)
			{
				throw new CommandLineArgsException("Invalid regex: {0}", pattern);
			}

			if (include)
				TagMatcher.AddIncludeRule(regex);
			else
				TagMatcher.AddExcludeRule(regex);
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
				var line = Regex.Replace(rawLine, @"#.*$", "");
				if (Regex.IsMatch(line, @"^\s*$"))
					continue;

				var match = Regex.Match(line, @"^(\S+)\s+(.*)");
				if (!match.Success)
					throw new CommandLineArgsException("{0}({1}): unrecognised input", filename, lineNo);

				yield return String.Format("--{0}", match.Groups[1].Value);
				yield return match.Groups[2].Value.Trim();
			}
		}

		void IncludeTag_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			AddTagRule(e.NewItems[0] as string, true);
		}

		void ExcludeTag_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			AddTagRule(e.NewItems[0] as string, false);
		}

		void Config_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Parse config files as they're encountered
			if (e.Action == NotifyCollectionChangedAction.Add)
				ParseConfigFile(e.NewItems[0] as string);
		}
	}
}