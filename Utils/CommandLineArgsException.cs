/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CvsGitConverter.Utils
{
	/// <summary>
	/// Exception thrown when the arguments passed to an application are invalid.
	/// </summary>
	class CommandLineArgsException : Exception
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