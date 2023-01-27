namespace Stratis.DevEx.Base.Tests
{
    using System.IO;

    public class LoggerTests
    {
        #region Constructors
        public LoggerTests ()
        {
            filelogger = new FileLogger("log1.log", false, "LoggerTests");
        }
        #endregion

        #region Tests
        [Fact]
        public void CanCreateFileLogger()
        {
            Assert.NotNull(filelogger);
            filelogger.Info("CanCreateFileLogger");
            Assert.True(File.Exists("log1.log"));
        }
        #endregion

        #region Fields
        FileLogger filelogger;
        #endregion

    }
}
