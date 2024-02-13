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
using Microsoft.SqlServer.Server;

namespace SourceGenerator.VsEditor
{
    internal class SourceGeneratorClassifier : IClassifier
    {
        public const string SERVER_IP = "127.0.0.1";
        public const int PORT = 58994;

        private ITextBuffer text;
        private string filePath;
        private readonly Dictionary<string, IClassificationType> types = new Dictionary<string, IClassificationType>();

        private CancellationTokenSource cancel;
        private SemaphoreSlim cancelMutex = new SemaphoreSlim(1, 1);

        private List<MatchSpan> spans = null;

        internal SourceGeneratorClassifier(IClassificationTypeRegistryService registry, ITextBuffer text)
        {
            this.text = text;
            types = Enum.GetNames(typeof(ClassType))
                .ToDictionary(
                    (it) => it,
                    (it) => registry.GetClassificationType(it));

            ITextDocument textDocument;
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
                            ReCalculateSpans(filePath, ev.After.GetText());
                            ClassificationChanged.Invoke(null, new ClassificationChangedEventArgs(new SnapshotSpan(ev.After, 0, ev.AfterVersion.Length)));
                        }
                    } finally
                    {
                        cancelMutex.Release();
                    }
                });
            };
        }

        private void ReCalculateSpans(string filePath, string source)
        {
            var ip = new IPEndPoint(IPAddress.Parse(SERVER_IP), PORT);
            using (var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(ip);

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
                        sourceText.Write(recvBuffer, 0, readBytes);
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
            if (spans is null)
            {
                ReCalculateSpans(filePath, text.CurrentSnapshot.GetText());
            }

            return spans
                .SkipWhile((it) => it.s + it.l <= span.Span.Start)
                .TakeWhile((it) => it.s <= span.Span.End)
                .Select((it) =>
                    new ClassificationSpan(new SnapshotSpan(
                        span.Snapshot,
                        new Span(it.s, it.l)),
                        types[it.c.ToString()]))
                .ToList();
        }
    }
}
