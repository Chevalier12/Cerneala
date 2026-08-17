namespace Cerneala.VisualStudio;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

internal static class CernealaContentType
{
    public const string Name = "cerneala-crn";

#pragma warning disable CS0649
    [Export]
    [Name(Name)]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    internal static ContentTypeDefinition Definition = null!;

    [Export]
    [FileExtension(".crn")]
    [ContentType(Name)]
    internal static FileExtensionToContentTypeDefinition FileExtension = null!;
#pragma warning restore CS0649
}
