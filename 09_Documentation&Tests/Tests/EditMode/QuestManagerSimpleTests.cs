using NUnit.Framework;

public class QuestManagerSimpleTests
{
    [Test]
    public void AddQuest_NullOrEmptyId_DoesNothing()
    {
        var qm = new QuestManagerSimple();

        qm.AddQuest(null);
        qm.AddQuest(new QuestDefinition { id = "" });

        Assert.IsNull(qm.Get("any"));
    }

    [Test]
    public void PlantingSeed_AdvancesOnlySeedQuest_AndCompletesAtTarget()
    {
        var qm = new QuestManagerSimple();

        qm.AddQuest(new QuestDefinition
        {
            id = "q_plant_1",
            title = "Посадить 1 семечко",
            targetTag = QuestTags.SeedPlanted,
            target = 1
        });

        qm.AddQuest(new QuestDefinition
        {
            id = "q_earn_100",
            title = "Заработать 100 монет",
            targetTag = QuestTags.MoneyEarned,
            target = 100
        });

        qm.OnPlantedSeed();

        var plant = qm.Get("q_plant_1");
        var earn = qm.Get("q_earn_100");

        Assert.NotNull(plant);
        Assert.NotNull(earn);

        Assert.AreEqual(1, plant.current);
        Assert.IsTrue(plant.completed);

        Assert.AreEqual(0, earn.current);
        Assert.IsFalse(earn.completed);
    }

    [Test]
    public void EarnMoney_Accumulates_AndCompletesAtTarget()
    {
        var qm = new QuestManagerSimple();

        qm.AddQuest(new QuestDefinition
        {
            id = "q_earn_10",
            title = "Заработать 10 монет",
            targetTag = QuestTags.MoneyEarned,
            target = 10
        });

        qm.OnEarnedMoney(3);
        Assert.AreEqual(3, qm.Get("q_earn_10").current);
        Assert.IsFalse(qm.Get("q_earn_10").completed);

        qm.OnEarnedMoney(7);
        Assert.AreEqual(10, qm.Get("q_earn_10").current);
        Assert.IsTrue(qm.Get("q_earn_10").completed);
    }
}
