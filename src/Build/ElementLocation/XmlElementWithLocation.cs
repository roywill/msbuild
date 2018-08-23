// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Improvement to XmlElement that during load attaches location information to itself.</summary>
//-----------------------------------------------------------------------

using Microsoft.Build.Shared;
using System;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.Build.Construction
{
    /// <summary>
    /// Derivation of XmlElement to implement IXmlLineInfo
    /// </summary>
    /// <remarks>
    /// It would be nice to add some helper overloads of base class members that
    /// downcast their return values to XmlElement/AttributeWithLocation. However
    /// C# doesn't currently allow covariance in method overloading, only on delegates.
    /// The caller must bravely downcast.
    /// </remarks>
    internal class XmlElementWithLocation : XmlElement, IXmlLineInfo, IXmlLineSpanInfo
    {
        WeakReference<ElementLocation> _elementLocation = new WeakReference<ElementLocation>(null);

        /// <summary>
        /// Constructor without location information
        /// </summary>
        public XmlElementWithLocation(string prefix, string localName, string namespaceURI, XmlDocumentWithLocation document)
            : base(prefix, localName, namespaceURI, document)
        {
        }

        /// <summary>
        /// Constructor with location information
        /// </summary>
        public XmlElementWithLocation(string prefix, string localName, string namespaceURI, XmlDocumentWithLocation document, int lineNumber, int columnNumber)
            : this(prefix, localName, namespaceURI, document)
        {
            // Subtract one, just to give the same value as the old code did.
            // In the past we pointed to the column of the open angle bracket whereas the XmlTextReader points to the first character of the element name.
            // In well formed XML these are always adjacent on the same line, so it's safe to subtract one.
            // If we're loading from a stream it's zero, so don't subtract one.
            XmlDocumentWithLocation documentWithLocation = (XmlDocumentWithLocation)document;

            int adjustedColumn = (columnNumber == 0) ? columnNumber : columnNumber - 1;

            this.LineNumber = lineNumber;
            this.LinePosition = adjustedColumn;
            this.EndLineNumber = lineNumber;
            this.EndLinePosition = adjustedColumn;
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
        /// Provides an ElementLocation for this element, using the path to the file containing
        /// this element as the project file entry.
        /// Element location may be incorrect, if it was not loaded from disk.
        /// Does not return null.
        /// </summary>
        /// <remarks>
        /// Should have at least the file name if the containing project has been given a file name,
        /// even if it wasn't loaded from disk, or has been edited since. That's because we set that
        /// path on our XmlDocumentWithLocation wrapper class.
        /// </remarks>
        internal ElementLocation Location
        {
            get
            {
                var fullPath = ((XmlDocumentWithLocation)OwnerDocument).FullPath;

                if (!_elementLocation.TryGetTarget(out ElementLocation location) || !string.Equals(location.File, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    location = ElementLocation.Create(fullPath, LineNumber, LinePosition, this.EndLineNumber - LineNumber, this.EndLinePosition);
                    _elementLocation.SetTarget(location);
                }

                return location;
            }
        }

        /// <summary>
        /// Returns the XmlAttribute with the specified name or null if a matching attribute was not found.
        /// </summary>
        public XmlAttributeWithLocation GetAttributeWithLocation(string name) => (XmlAttributeWithLocation)GetAttributeNode(name);

        /// <summary>
        /// Overridden to convert the display of the element from open form (separate open and closed tags) to closed form 
        /// (single closed tag) if the last child is being removed. This is simply for tidiness of the project file.
        /// For example, removing the only piece of metadata from an item will leave behind one tag instead of two.
        /// </summary>
        public override XmlNode RemoveChild(XmlNode oldChild)
        {
            XmlNode result = base.RemoveChild(oldChild);

            if (!HasChildNodes)
            {
                IsEmpty = true;
            }

            return result;
        }

        /// <summary>
        /// Gets the location of any attribute on this element with the specified name.
        /// If there is no such attribute, returns null.
        /// </summary>
        internal ElementLocation GetAttributeLocation(string name) => GetAttributeWithLocation(name)?.GetLocation();
    }
}
