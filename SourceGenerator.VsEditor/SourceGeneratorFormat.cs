using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace SourceGenerator.VsEditor
{
    /// <summary>
    /// Defines an editor format for the SourceGeneratorClassifier type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = nameof(SourceGeneratorClassifier))]
    [Name(nameof(SourceGeneratorClassifier))]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class SourceGeneratorFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceGeneratorFormat"/> class.
        /// </summary>
        public SourceGeneratorFormat()
        {
            DisplayName = nameof(SourceGeneratorClassifier); // Human readable version of the name
            BackgroundColor = Colors.BlueViolet;
            TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }
}
