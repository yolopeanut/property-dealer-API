using property_dealer_API.Core.Logic.GameRulesManager;


namespace PropertyDealer.API.Tests
{
    public class SimpleTest
    {
        [Fact]
        public void CanCreateGameRuleManager()
        {
            var manager = new GameRuleManager();
            Assert.NotNull(manager);
        }
    }
}