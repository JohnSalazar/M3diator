namespace M3diator.Tests;
public class UnitTests
{
    [Fact]
    public void Unit_Value_ShouldBeDefault()
    {
        Assert.Equal(default, Unit.Value);
    }

    [Fact]
    public void Unit_Equals_ShouldReturnTrueForSameType()
    {
        var unit1 = Unit.Value;
        var unit2 = new Unit();
        object objUnit = Unit.Value;

        Assert.True(unit1.Equals(unit2));
        Assert.True(unit1 == unit2);
        Assert.False(unit1 != unit2);
        Assert.True(unit1.Equals(objUnit));
        Assert.Equal(0, unit1.CompareTo(unit2));
        Assert.Equal(0, unit1.GetHashCode());
        Assert.Equal("()", unit1.ToString());
    }

    [Fact]
    public async Task Unit_Task_ShouldReturnCompletedTaskWithUnitValue()
    {
        var result = await Unit.Task;
        Assert.Equal(Unit.Value, result);
        Assert.True(Unit.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Unit_StressTest_MassiveConcurrency_ShouldRemainConsistent()
    {
        const int iterations = 100_000;
        var tasks = new Task[iterations];

        for (int i = 0; i < iterations; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var result = await Unit.Task;
                Assert.Equal(Unit.Value, result);
                Assert.True(result == Unit.Value);
                Assert.False(result != Unit.Value);
                Assert.True(result.Equals(Unit.Value));
            });
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public void Unit_Task_ShouldAlwaysBeCompletedSuccessfully_UnderStress()
    {
        const int iterations = 100_000;

        Parallel.For(0, iterations, i =>
        {
            Assert.True(Unit.Task.IsCompletedSuccessfully);
            Assert.Equal(Unit.Value, Unit.Task.Result);
        });
    }

}
