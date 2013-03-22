/*
 * John.Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;

namespace CvsGitImporter.Utils
{
	/// <summary>
	/// Exception thrown when the arguments passed to an application are invalid.
	/// </summary>
	public class CommandLineArgsException : Exception
	{
		#region Constructors

		public CommandLineArgsException(string message) : base(message)
		{
		}

		public CommandLineArgsException(string format, params object[] args) : base(String.Format(format, args))
		{
		}

		#endregion
	}
}
