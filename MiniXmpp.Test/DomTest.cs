﻿using MiniXmpp.Dom;
using MiniXmpp.Enums;
using Xunit.Abstractions;

namespace MiniXmpp.Test;

public class DomTest(ITestOutputHelper output)
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

        Assert.Equal("foo", el.TagName);
        Assert.Equal("baz", el.Attributes["bar"]);
    }

    [Fact]
    public void ParseSimpleXml()
    {
        var xml = "<foo xmlns='bar'><child1 /><!--some comment--></foo>";
        var root = Xml.Parse(xml);

        Assert.NotNull(root);
        Assert.Equal("foo", root.TagName);
        Assert.Equal("bar", root.GetNamespace());

        var nodes = root.Nodes();

        var firstNode = nodes.FirstOrDefault();
        Assert.NotNull(firstNode);
        Assert.IsType<XmppElement>(firstNode);

        var theElement = (XmppElement)firstNode;
        Assert.Equal("child1", theElement.TagName);

        var lastNode = nodes.LastOrDefault();
        Assert.NotNull(lastNode);
        Assert.IsType<XmppComment>(lastNode);

        var comment = (XmppComment)lastNode;
        Assert.Equal("some comment", comment.Value);
    }

    [Fact]
    public void ToStringTest()
    {
        var xml = "<foo xmlns='bar'><child1 /><!--some comment--></foo>";
        var element = Xml.Parse(xml);
        Assert.NotNull(element);
        output.WriteLine(element.ToString());
    }

    [Fact]
    public void ToStringFormattedTest()
    {
        var xml = "<foo xmlns='bar'><child1 /><!--some comment--></foo>";
        var element = Xml.Parse(xml);
        Assert.NotNull(element);
        output.WriteLine(element.ToString(true));
    }

    [Fact]
    public void DumpStreamErrorTest()
    {
        var se = Xml.StreamError(StreamErrorCondition.HostUnknown);
        Assert.Equal("stream", se.Prefix);
        Assert.Equal(Namespaces.Stream, se.GetNamespace("stream"));
        Assert.Equal("host-unknown", se.Elements().First().TagName);
    }

    [Fact]
    public void ShouldThrowInvalidXmppName()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            XmppName test = "";
            output.WriteLine(test);
        });

        Assert.ThrowsAny<Exception>(() =>
        {
            string? s = null;
            XmppName test = s!;
            output.WriteLine(test);
        });

        Assert.ThrowsAny<Exception>(() =>
        {
            XmppName test = "a:";
            output.WriteLine(test);
        });

        XmppName test2 = ":b";
        Assert.Equal("b", test2.LocalName);
    }

    [Fact]
    public void ParseNullXml()
    {
        var xml = "";
        var root = Xml.Parse(xml);
        Assert.Null(root);
    }

    [Fact]
    public void ParseRealWorldXml()
    {
        // Source: https://www.bing.com/search?q=xml+sample&PC=U316&FORM=CHROMN

        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<note>
<to>Tove</to>
<from>Jani</from>
<heading>Reminder</heading>
<body>Don't forget me this weekend!</body>
</note>";
        var root = Xml.Parse(xml);

        Assert.NotNull(root);
        Assert.Equal("note", root.TagName);

        output.WriteLine(root.ToString(true));
    }
}
