using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using static SourceGenerator.VsEditor.SourceGeneratorClassificationDefinition;

namespace SourceGenerator.VsEditor
{
    internal class SourceGeneratorClassifier : IClassifier
    {
        private readonly Dictionary<string, IClassificationType> types = new Dictionary<string, IClassificationType>();

        private CancellationTokenSource cancel;
        private SemaphoreSlim cancelMutex = new SemaphoreSlim(1, 1);

        internal SourceGeneratorClassifier(IClassificationTypeRegistryService registry, ITextBuffer text)
        {
            types[nameof(PlainText)] = registry.GetClassificationType(nameof(PlainText));
            types[nameof(TopLevel)] = registry.GetClassificationType(nameof(TopLevel));

            text.Changed += (_, ev) =>
            {
                _ = Task.Run(async () =>
                {
                    CancellationTokenSource _cancel;
                    await cancelMutex.WaitAsync();
                    try
                    {
                        cancel?.Cancel();
                        cancel?.Dispose();
                        _cancel = cancel = new CancellationTokenSource();
                    } finally
                    {
                        cancelMutex.Release();
                    }

                    try
                    {
                        await Task.Delay(500, _cancel.Token);
                    } catch (TaskCanceledException)
                    {
                    }

                    await cancelMutex.WaitAsync();
                    try
                    {
                        if (!_cancel.IsCancellationRequested)
                        {
                            await ReCalculateSpansAsync();
                            ClassificationChanged.Invoke(null, new ClassificationChangedEventArgs(new SnapshotSpan(ev.After, 0, ev.AfterVersion.Length)));
                        }
                    } finally
                    {
                        cancelMutex.Release();
                    }
                });
            };
        }

        private async Task ReCalculateSpansAsync()
        {
            await Task.CompletedTask;
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

        private static bool toggle;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return new List<ClassificationSpan>()
            {
                new ClassificationSpan(new SnapshotSpan(span.Snapshot, new Span(span.Start, span.Length)), types[(toggle = !toggle) ? nameof(TopLevel) : nameof(PlainText)])
            };
        }
    }
}
