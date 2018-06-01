using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Microsoft.Build.Internal
{
    /// <summary>
    /// Specialized XmlReader that fires EndElement events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Using XmlReader directly obviates the need for this class, but hooking into XmlDocument.Load results in no way of knowing
    /// where the end of an element is.  It is buried under the XmlLoader which is created within XmlDocument.</para>
    /// <para>
    /// Once the end element is detected, there is an added challenge of figuring out where in the stream that location is.  This is buried
    /// inside XmlTextReader in an instance of XmlReaderImpl.  To surface that information, we need to leverage reflection to access the internal
    /// property for Impl.  Once that is found, we then need to find the properties (via reflection) that allow us to compute the current
    /// file line and offset within that line.</para>
    /// <para>
    /// It is possible to get the absolute position of the end of an element (or even the end of any node), but the internal implementation of 
    /// IXmlLineInfo only snaps the start line and line offset.  As a result, we currently capture only the end position in the same units.</para>
    /// </remarks>
    internal class XmlReaderWithSpan : XmlTextReader
    {
        public delegate void XmlNodeCompleteEventHandler(object sender, EndElementEventArgs e);

        public class EndElementEventArgs : EventArgs
        {
            public string Name;
            public int StartLine;
            public int StartLineOffset;
            public int EndLine;
            public int EndLineOffset;
        }

        #region Binding information to internal non-directly accessible fields.
        /// <summary>
        /// The information to access the LineNo property off of an XmlReaderImpl.
        /// </summary>
        private static PropertyInfo xrLineNo;

        /// <summary>
        /// The information to acces the line start position off of an XmlReaderImpl.
        /// </summary>
        private static PropertyInfo xrLineStartPosition;

        /// <summary>
        /// The information to access the current stream position off of an XmlReaderImpl.
        /// </summary>
        private static PropertyInfo xrCurrentPosition;
        #endregion

        /// <summary>
        /// Current list of EndElement event consumers.
        /// </summary>
        private XmlNodeCompleteEventHandler onNodeCompleteDelegate;

        /// <summary>
        /// Cached reference to internal implementation of the XmlReader.
        /// </summary>
        private XmlReader implementation;

        public XmlReaderWithSpan(string url, TextReader input) : base(url, input)
        {
        }

        public override bool Read()
        {
            var rc = base.Read();
            if (!rc)
            {
                return rc;
            }

            if (this.NodeType == XmlNodeType.EndElement && this.onNodeCompleteDelegate != null)
            {
                this.onNodeCompleteDelegate(this, new EndElementEventArgs { Name = this.Name, StartLine = this.LineNumber, StartLineOffset = this.LinePosition, EndLine = this.CurrentLineNumber, EndLineOffset = this.CurrentLinePosition });
            }

            return rc;
        }

        public event XmlNodeCompleteEventHandler NodeComplete
        {
            add
            {
                onNodeCompleteDelegate += value;
            }
            remove
            {
                onNodeCompleteDelegate -= value;
            }
        }

        #region Position extensions for computing element span ranges.
        /// <summary>
        /// Get the current line offset in the source stream.
        /// </summary>
        /// <remarks>
        /// <para>The XmlReader normally returns only the start of an element tag and does not give information of the end.</para>
        /// </remarks>
        public int CurrentLinePosition => (int)(CurrentPosition - this.CurrentLineStartOffset);

        /// <summary>
        /// Get the current line number in the source stream.
        /// </summary>
        /// <remarks>
        /// <para>The XmlReader normally returns only the start of an element tag and does not give information of the end.</para>
        /// </remarks>
        public int CurrentLineNumber
        {
            get
            {
                if (xrLineNo == null)
                {
                    var temp = this.Implementation.GetType().GetProperty("DtdParserProxy_LineNo", BindingFlags.Instance | BindingFlags.NonPublic);
                    Interlocked.Exchange(ref xrLineNo, temp);
                }

                return (int)xrLineNo.GetValue(this.Implementation);
            }
        }

        /// <summary>
        /// Get the physical offset to the start of the current line offset in the source stream.
        /// </summary>
        /// <remarks>
        /// <para>The XmlReader normally returns only the start of an element tag and does not give information of the end.</para>
        /// </remarks>
        private long CurrentLineStartOffset
        {
            get
            {
                if (xrLineStartPosition == null)
                {
                    var temp = this.Implementation.GetType().GetProperty(@"DtdParserProxy_LineStartPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                    Interlocked.Exchange(ref xrLineStartPosition, temp);
                }

                return (int)xrLineStartPosition.GetValue(this.Implementation);
            }
        }

        /// <summary>
        /// Get the current offset in the source stream.
        /// </summary>
        /// <remarks>
        /// <para>The XmlReader normally returns only the start of an element tag and does not give information of the end.</para>
        /// </remarks>
        private int CurrentPosition
        {
            get
            {
                if (xrCurrentPosition == null)
                {
                    var temp = this.Implementation.GetType().GetProperty(@"DtdParserProxy_CurrentPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                    Interlocked.Exchange(ref xrCurrentPosition, temp);
                }

                return (int)xrCurrentPosition.GetValue(this.Implementation);

            }
        }

        /// <summary>
        /// Get the XmlReader internal implementation object.
        /// </summary>
        /// <remarks>
        /// The current XmlTextReader leverages an internal implementation that does the work.  All information about state and
        /// progress are thus stored there.  To bubble out that additional information thus you first need to obtain a pointer
        /// to this instance.
        /// </remarks>
        private XmlReader Implementation
        {
            get
            {
                if (this.implementation == null)
                {

                    var temp = GetType().GetProperty("Impl", BindingFlags.Instance | BindingFlags.NonPublic);
                    Interlocked.Exchange(ref implementation, (XmlReader)(temp?.GetValue(this)) ?? this);
                }

                return this.implementation;
            }
        }
        #endregion
    }
}
