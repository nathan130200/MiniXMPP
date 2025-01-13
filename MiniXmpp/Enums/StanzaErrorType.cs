using MiniXmpp.Attributes;

namespace MiniXmpp.Enums;

public enum StanzaErrorType
{
    [XmppMember("auth")]
    Auth,

    [XmppMember("cancel")]
    Cancel,

    [XmppMember("continue")]
    Continue,

    [XmppMember("modify")]
    Modify,

    [XmppMember("wait")]
    Wait
}
