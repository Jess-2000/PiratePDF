using BlackFlag.Engine.Document.Outlines;

namespace BlackFlag.Engine.Document.Tests.Outlines;

public sealed class PdfOutlineModelTests
{
    [Fact]
    public void NewOutlineStartsEmpty()
    {
        var outline = new PdfOutline();

        Assert.Empty(outline.Items);
    }

    [Fact]
    public void ChildHierarchyPreservesIdentityAndRejectsCycles()
    {
        var parent = new PdfOutlineItem("Parent");
        var child = new PdfOutlineItem("Child");

        parent.InsertChild(0, child);

        Assert.Same(child, Assert.Single(parent.Children));
        Assert.Same(parent, child.Parent);
        Assert.Throws<InvalidOperationException>(
            () => child.InsertChild(0, parent));
    }

    [Fact]
    public void NavigationActionValidatesTypeAndTarget()
    {
        var action = new PdfOutlineNavigationAction(
            PdfOutlineNavigationActionType.Named,
            "NextPage");

        Assert.Equal(PdfOutlineNavigationActionType.Named, action.ActionType);
        Assert.Equal("NextPage", action.Target);
        Assert.Throws<ArgumentException>(
            () => new PdfOutlineNavigationAction(
                PdfOutlineNavigationActionType.Uri,
                " "));
    }
}
