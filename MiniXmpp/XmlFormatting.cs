﻿namespace MiniXmpp;

[Flags]
public enum XmlFormatting
{
    None,
    Indented = 1 << 0,
    OmitDuplicatedNamespaces = 1 << 1,
    OmitXmlDeclaration = 1 << 2,
    NewLineOnAttributes = 1 << 3,
    DoNotEscapeUriAttributes = 1 << 4,
    CheckCharacters = 1 << 5,
    WriteEndDocumentOnClose = 1 << 6,

    Default
        = OmitDuplicatedNamespaces
        | OmitXmlDeclaration
        | CheckCharacters
        | WriteEndDocumentOnClose,
}