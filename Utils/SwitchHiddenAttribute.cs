/*
 * John.Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;

namespace CvsGitImporter.Utils
{
	/// <summary>
	/// Mark a switch as hidden, i.e. one that is undocumented.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
	public class SwitchHiddenAttribute : Attribute
	{
		public SwitchHiddenAttribute()
		{
		}
	}
}