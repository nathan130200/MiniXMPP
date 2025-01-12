namespace MiniXmpp.Test;

public class DomTest
{
    [Fact]
    public void SimpleElement()
    {
        var el = new XmppElement("foo")
        {
            Attributes =
            {
                ["bar"] = "baz"
            }
        };
    }
}
