using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
				throw new ArgumentException("Need a cvs.log file");

			var parser = new CvsLogParser(args[0]);
			var commits = parser.Parse();

			var changeSets = new Dictionary<string, ChangeSet>();

			foreach (var commit in commits)
			{
				ChangeSet changeSet;
				if (changeSets.TryGetValue(commit.CommitId, out changeSet))
				{
					changeSet.Add(commit);
				}
				else
				{
					changeSet = new ChangeSet(commit.CommitId) { commit };
					changeSets.Add(changeSet.CommitId, changeSet);
				}
			}

			foreach (var changeSet in changeSets.Values.OrderBy(c => c.Time))
			{
				Console.Out.WriteLine("Commit: {0} {1}", changeSet.CommitId, changeSet.Time);
				foreach (var commit in changeSet)
					Console.Out.WriteLine("  {0} r{1}", commit.File, commit.Revision);
			}
		}
	}
}