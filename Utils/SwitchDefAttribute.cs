/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Diagnostics;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Mark a property as a commandline argument, and specifies what switches apply.
	/// Applies to properties in classes derived from CommandLineParamsBase.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
	class SwitchDefAttribute : Attribute
	{
		#region Public members

		/// <summary>
		/// The short switch for this argument, e.g. '-d'. May be null.
		/// </summary>
		public string ShortSwitch
		{
			[DebuggerStepThrough] get; [DebuggerStepThrough] set;
		}

		/// <summary>
		/// The long switch for this argument, e.g. '--debug'. May be null.
		/// </summary>
		public string LongSwitch
		{
			[DebuggerStepThrough] get; [DebuggerStepThrough] set;
		}

		/// <summary>
		/// The help description for this switch.
		/// </summary>
		public string Description
		{
			[DebuggerStepThrough] get; [DebuggerStepThrough] set;
		}

		/// <summary>
		/// A string that is inserted for the value in the help description.
		/// </summary>
		public string ValueDescription
		{
			[DebuggerStepThrough] get; [DebuggerStepThrough] set;
		}

		#endregion


		#region Constructors

		public SwitchDefAttribute()
		{
		}

		public SwitchDefAttribute(string shortForm, string longForm)
		{
			this.ShortSwitch = shortForm;
			this.LongSwitch = longForm;
		}

		#endregion


		public override string ToString()
		{
			return String.Format("{{CommandLineArgAttribute{0}{1}}}",
					(ShortSwitch == null) ? "" : " " + ShortSwitch, (LongSwitch == null) ? "" : " " + LongSwitch);
		}
	}
}