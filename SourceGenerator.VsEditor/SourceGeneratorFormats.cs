using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Documents;
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
    [ClassificationType(ClassificationTypeNames = nameof(Delimeter))]
    [Name(nameof(DelimeterFormat))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class DelimeterFormat : ClassificationFormatDefinition
    {
        public DelimeterFormat()
        {
            DisplayName = nameof(Delimeter);
            ForegroundColor = Colors.DeepPink;//LightSteelBlue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Delimeter2))]
    [Name(nameof(Delimeter2Format))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class Delimeter2Format : ClassificationFormatDefinition
    {
        public Delimeter2Format()
        {
            DisplayName = nameof(Delimeter2);
            ForegroundColor = Colors.LawnGreen;//Orange;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(Delimeter3))]
    [Name(nameof(Delimeter3Format))]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class Delimeter3Format : ClassificationFormatDefinition
    {
        public Delimeter3Format()
        {
            DisplayName = nameof(Delimeter3);
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
