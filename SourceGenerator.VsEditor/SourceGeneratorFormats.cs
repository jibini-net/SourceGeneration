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
            ForegroundColor = Colors.PowderBlue;
            IsItalic = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Delimiter))]
    [Name(nameof(DelimiterFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class DelimiterFormat : ClassificationFormatDefinition
    {
        public DelimiterFormat()
        {
            DisplayName = nameof(Delimiter);
            ForegroundColor = Colors.DeepPink;//LightSteelBlue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Delimiter2))]
    [Name(nameof(Delimiter2Format))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class Delimiter2Format : ClassificationFormatDefinition
    {
        public Delimiter2Format()
        {
            DisplayName = nameof(Delimiter2);
            ForegroundColor = Colors.LawnGreen;//Orange;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Delimiter3))]
    [Name(nameof(Delimiter3Format))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class Delimiter3Format : ClassificationFormatDefinition
    {
        public Delimiter3Format()
        {
            DisplayName = nameof(Delimiter3);
            ForegroundColor = Colors.Orange;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Assign))]
    [Name(nameof(AssignFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class AssignFormat : ClassificationFormatDefinition
    {
        public AssignFormat()
        {
            DisplayName = nameof(Assign);
            ForegroundColor = Colors.DarkOrange;
            IsItalic = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(TypeName))]
    [Name(nameof(TypeFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class TypeFormat : ClassificationFormatDefinition
    {
        public TypeFormat()
        {
            DisplayName = nameof(TypeName);
            ForegroundColor = Colors.LawnGreen;//Colors.DarkOrange;
            TextDecorations = System.Windows.TextDecorations.Underline;
            IsBold = true;
        }
    }
}
