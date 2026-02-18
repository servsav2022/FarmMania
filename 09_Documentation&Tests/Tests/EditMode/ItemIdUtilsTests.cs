using NUnit.Framework;

public class ItemIdUtilsTests
{
    [Test]
    public void Normalize_NullOrEmpty_ReturnsEmpty()
    {
        Assert.AreEqual("", ItemIdUtils.Normalize(null));
        Assert.AreEqual("", ItemIdUtils.Normalize(""));
        Assert.AreEqual("", ItemIdUtils.Normalize("   "));
    }

    [Test]
    public void Normalize_TrimsLowercases_RemovesClone()
    {
        Assert.AreEqual("carrot", ItemIdUtils.Normalize("  CARROT  "));
        Assert.AreEqual("carrot", ItemIdUtils.Normalize("Carrot(Clone)"));
        Assert.AreEqual("carrot_seed", ItemIdUtils.Normalize("Carrot   Seed"));
    }
}
