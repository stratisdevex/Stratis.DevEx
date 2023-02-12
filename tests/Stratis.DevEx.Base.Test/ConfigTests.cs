namespace Stratis.DevEx.Base.Test
{
    using SharpConfig;
    using Stratis.DevEx;

    public class ConfigTests
    {
        public ConfigTests()
        {
            cfg1 = Runtime.LoadConfig("Stratis.DevEx.cfg");
        }
        [Fact]
        public void CanBindConfig()
        {
            Assert.NotNull(cfg1);
            var cfg2 = Runtime.CreateDefaultConfig();
            var c = Runtime.BindConfig(cfg2, cfg1);
            Assert.True(c["General"]["Debug"].BoolValue);
        }

        Configuration cfg1;
    }
}