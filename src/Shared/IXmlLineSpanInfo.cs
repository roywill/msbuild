// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>An internal interface used to obtain an element location (start and end).</summary>
//-----------------------------------------------------------------------

using System.Xml;

namespace Microsoft.Build.Shared
{
    /// <summary>
    /// The Element start and end position.
    /// </summary>
    public interface IXmlLineSpanInfo : IXmlLineInfo
    {
        /// <summary>
        /// The line number where the end of an element is.
        /// </summary>
        int EndLineNumber
        {
            get;
        }

        /// <summary>
        /// The position on the line where the end element character &gt; is.
        /// </summary>
        int EndLinePosition
        {
            get;
        }
    }
}
