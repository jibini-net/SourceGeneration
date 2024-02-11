using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

using static SourceGenerator.VsEditor.SourceGeneratorClassificationDefinition;

namespace SourceGenerator.VsEditor
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(PlainText))]
    [Name(nameof(PlainTextFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class PlainTextFormat : ClassificationFormatDefinition
    {
        public PlainTextFormat()
        {
            DisplayName = nameof(PlainText);
            ForegroundColor = Colors.GhostWhite;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(TopLevel))]
    [Name(nameof(TopLevelFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class TopLevelFormat : ClassificationFormatDefinition
    {
        public TopLevelFormat()
        {
            DisplayName = nameof(TopLevel);
            ForegroundColor = Colors.BlueViolet;
            TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }
}
