/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Rhino.Mocks;
using Rhino.Mocks.Interfaces;

namespace CTC.CvsntGitImporter.TestCode.Utils
{
	/// <summary>
	/// Extensions methods for Rhino Mocks.
	/// </summary>
	public static class RhinoMocksExtensions
	{
		/// <summary>
		/// Perform an action when a method invocation is matched.
		/// </summary>
		public static IMethodOptions<object> Do(this IMethodOptions<object> options, Action action)
		{
			return options.Do((Delegate)action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<object> Do<T1>(this IMethodOptions<object> options, Action<T1> action)
		{
			return options.Do(action);
		}

		/// <summary>
		/// Perform an action when a method invocation is matched.
		/// </summary>
		public static IMethodOptions<TResult> Do<TResult>(this IMethodOptions<TResult> options, Func<TResult> action)
		{
			return options.Do((Delegate)action);
		}

		/// <summary>
		/// Perform an action when a method invocation is matched.
		/// </summary>
		public static IMethodOptions<TResult> Do<T1, TResult>(this IMethodOptions<TResult> options, Func<T1, TResult> action)
		{
			return options.Do((Delegate)action);
		}

		/// <summary>
		/// Perform an action when a method invocation is matched.
		/// </summary>
		public static IMethodOptions<TResult> Do<T1, T2, TResult>(this IMethodOptions<TResult> options, Func<T1, T2, TResult> action)
		{
			return options.Do((Delegate)action);
		}

		public static IMethodOptions<object> Return<T1>(this IMethodOptions<object> options, Func<T1> action)
		{
			return options.Return(action());
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<object> AlwaysDo(this IMethodOptions<object> options, Action action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<object> AlwaysDo<T1>(this IMethodOptions<object> options, Action<T1> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<object> AlwaysDo<T1, T2>(this IMethodOptions<object> options, Action<T1, T2> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<object> AlwaysDo<T1, T2, T3>(this IMethodOptions<object> options, Action<T1, T2, T3> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<TResult>(this IMethodOptions<TResult> options, Func<TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<T1, TResult>(this IMethodOptions<TResult> options, Func<T1, TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<T1, T2, TResult>(this IMethodOptions<TResult> options, Func<T1, T2, TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<T1, T2, T3, TResult>(this IMethodOptions<TResult> options, Func<T1, T2, T3, TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<T1, T2, T3, T4, TResult>(this IMethodOptions<TResult> options, Func<T1, T2, T3, T4, TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}

		/// <summary>
		/// Allows a method to be stubbed with a supplied action.
		/// </summary>
		/// <remarks>This method is the same as Do but does not require IgnoreArguments() to be specified.</remarks>
		public static IMethodOptions<TResult> AlwaysDo<T1, T2, T3, T4, T5, TResult>(this IMethodOptions<TResult> options, Func<T1, T2, T3, T4, T5, TResult> action)
		{
			return options.IgnoreArguments().Do(action);
		}
	}
}
