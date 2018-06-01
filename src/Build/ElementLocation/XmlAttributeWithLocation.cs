// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Improvement to XmlAttribute that during load attaches location information to itself.</summary>
//-----------------------------------------------------------------------

using Microsoft.Build.Shared;
using System;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.Build.Construction
{
    /// <summary>
    /// Derivation of XmlAttribute to implement IXmlLineInfo
    /// </summary>
    internal class XmlAttributeWithLocation : XmlAttribute, IXmlLineSpanInfo
    {
        WeakReference<ElementLocation> _elementLocation = new WeakReference<ElementLocation>(null);

        /// <summary>
        /// Constructor without location information
        /// </summary>
        public XmlAttributeWithLocation(string prefix, string localName, string namespaceURI, XmlDocument document)
            : base(prefix, localName, namespaceURI, document)
        {
        }

        /// <summary>
        /// Constructor with location information
        /// </summary>
        public XmlAttributeWithLocation(string prefix, string localName, string namespaceURI, XmlDocument document, int lineNumber, int columnNumber)
            : this(prefix, localName, namespaceURI, document)
        {
            this.LineNumber = lineNumber;
            this.LinePosition = columnNumber;
            this.EndLineNumber = lineNumber;
            this.EndLinePosition = columnNumber;
        }

        #region IXmlLineSpanInfo
        #region IXmlLineInfo implementation
        /// <inheritdoc />
        public int LineNumber
        {
            get;
        }

        /// <inheritdoc />
        public int LinePosition
        {
            get;
        }

        /// <inheritdoc />
        public bool HasLineInfo()
        {
            return LineNumber != 0;
        }
        #endregion

        /// <inheritdoc />
        public int EndLineNumber
        {
            get;
            set;
        }

        /// <inheritdoc />
        public int EndLinePosition
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Provides an ElementLocation for this attribute.
        /// </summary>
        /// <remarks>
        /// Should have at least the file name if the containing project has been given a file name,
        /// even if it wasn't loaded from disk, or has been edited since. That's because we set that
        /// path on our XmlDocumentWithLocation wrapper class.
        /// </remarks>
        internal ElementLocation GetLocation()
        {
            var fullPath = ((XmlDocumentWithLocation)OwnerDocument).FullPath;

            if (!_elementLocation.TryGetTarget(out ElementLocation location) || !string.Equals(location.File, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                location = ElementLocation.Create(fullPath, LineNumber, LinePosition);
                _elementLocation.SetTarget(location);
            }

            return location;
        }
    }
}
