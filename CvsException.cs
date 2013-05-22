/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Runtime.Serialization;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// An exception thrown as a result of a CVS command
	/// </summary>
	[Serializable]
	class CvsException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <cref>CvsGitConverter.CvsException</cref> class.
		/// </summary>
		public CvsException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <cref>CvsGitConverter.CvsException</cref> class with a
		/// specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public CvsException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <cref>CvsGitConverter.CvsException</cref> class with a
		/// specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
		public CvsException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <cref>CvsGitConverter.CvsException</cref> class with serialized data.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected CvsException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}