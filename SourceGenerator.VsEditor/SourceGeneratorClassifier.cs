using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

using static SourceGenerator.VsEditor.SourceGeneratorClassificationDefinition;

namespace SourceGenerator.VsEditor
{
    internal class SourceGeneratorClassifier : IClassifier
    {
        public const string SERVER_IP = "127.0.0.1";
        public const int PORT = 58994;

        private readonly Dictionary<string, IClassificationType> types = new Dictionary<string, IClassificationType>();

        private CancellationTokenSource cancel;
        private SemaphoreSlim cancelMutex = new SemaphoreSlim(1, 1);

        private List<MatchSpan> spans = new List<MatchSpan>();

        internal SourceGeneratorClassifier(IClassificationTypeRegistryService registry, ITextBuffer text)
        {
            types[nameof(PlainText)] = registry.GetClassificationType(nameof(PlainText));
            types[nameof(TopLevel)] = registry.GetClassificationType(nameof(TopLevel));

            ITextDocument textDocument;
            string filePath = "";
            if (text.Properties.TryGetProperty(typeof(ITextDocument), out textDocument))
            {
                filePath = textDocument.FilePath;
            } else
            {
                throw new Exception("Missing text document as property");
            }

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
                            await ReCalculateSpansAsync(filePath, ev.After.GetText());
                            ClassificationChanged.Invoke(null, new ClassificationChangedEventArgs(new SnapshotSpan(ev.After, 0, ev.AfterVersion.Length)));
                        }
                    } finally
                    {
                        cancelMutex.Release();
                    }
                });
            };
        }

        private async Task ReCalculateSpansAsync(string filePath, string source)
        {
            var ip = new IPEndPoint(IPAddress.Parse(SERVER_IP), PORT);
            using (var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                await socket.ConnectAsync(ip);

                var fileName = Path.GetFileName(filePath);
                socket.Send(Encoding.UTF8.GetBytes($"highlight {{{fileName}}} "));
                socket.Send(Encoding.UTF8.GetBytes(source));
                socket.Send(new byte[] { 0x00 });

                var recvBuffer = new byte[2048];
                using (var sourceText = new MemoryStream())
                {
                    for (;;)
                    {
                        var readBytes = socket.Receive(recvBuffer);
                        if (readBytes == 0)
                        {
                            break;
                        }
                        await sourceText.WriteAsync(recvBuffer, 0, readBytes);
                    }

                    var result = Encoding.UTF8.GetString(sourceText.ToArray());
                    spans = JsonSerializer.Deserialize<List<MatchSpan>>(result);
                }
            }
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
            return spans
                .Select((it) =>
                    new ClassificationSpan(new SnapshotSpan(
                        span.Snapshot,
                        new Span(it.s, it.l)),
                        types[it.c.ToString()]))
                .ToList();
        }
    }
}
