using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

public class AutoMoqDataAttribute : AutoDataAttribute
{
    // The constructor creates a Fixture, customizes it with AutoMoq,
    // and then passes that fixture to the base AutoDataAttribute.
    public AutoMoqDataAttribute()
        : base(() =>
        {
            var fixture = new Fixture();
            // This is the key line:
            fixture.Customize(new AutoMoqCustomization());
            return fixture;
        })
    {
    }
}