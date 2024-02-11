﻿using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace SourceGenerator.VsEditor
{
    /// <summary>
    /// Classification type definition export for SourceGeneratorClassifier
    /// </summary>
    internal static class SourceGeneratorClassificationDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169
#pragma warning disable 649

        [Export]
        [Name("GeneratorModel")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition GeneratorModelTypeDefinition;

        [Export]
        [FileExtension(".model")]
        [ContentType("GeneratorModel")]
        internal static FileExtensionToContentTypeDefinition GeneratorModelFileExtensionDefinition;

        [Export]
        [Name("GeneratorView")]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition GeneratorViewTypeDefinition;

        [Export]
        [FileExtension(".view")]
        [ContentType("GeneratorView")]
        internal static FileExtensionToContentTypeDefinition GeneratorViewFileExtensionDefinition;

        /// <summary>
        /// Defines the "SourceGeneratorClassifier" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(nameof(SourceGeneratorClassifier))]
        private static ClassificationTypeDefinition typeDefinition;

#pragma warning restore 169
#pragma warning restore 649
    }
}
