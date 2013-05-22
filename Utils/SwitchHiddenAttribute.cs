/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Mark a switch as hidden, i.e. one that is undocumented.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
	class SwitchHiddenAttribute : Attribute
	{
		public SwitchHiddenAttribute()
		{
		}
	}
}