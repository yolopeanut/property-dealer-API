using property_dealer_API.Core.Logic.GameRulesManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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