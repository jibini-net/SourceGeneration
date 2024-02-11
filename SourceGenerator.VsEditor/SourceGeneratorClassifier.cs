using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;

using static SourceGenerator.VsEditor.SourceGeneratorClassificationDefinition;

namespace SourceGenerator.VsEditor
{
    internal class SourceGeneratorClassifier : IClassifier
    {
        private readonly Dictionary<string, IClassificationType> types = new Dictionary<string, IClassificationType>();

        internal SourceGeneratorClassifier(IClassificationTypeRegistryService registry)
        {
            types[nameof(PlainText)] = registry.GetClassificationType(nameof(PlainText));
            types[nameof(TopLevel)] = registry.GetClassificationType(nameof(TopLevel));
        }

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return Enumerable.Range(span.Start.Position, span.Length)
                .Select((i) => new ClassificationSpan(new SnapshotSpan(span.Snapshot, new Span(i, 1)), types[i % 2 == 0 ? nameof(TopLevel) : nameof(PlainText)]))
                .ToList();
        }
    }
}
