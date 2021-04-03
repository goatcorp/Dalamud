using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Dalamud.Test {
    [TestFixture]
    public class LocalizationTests {
        private Localization localization;
        private string currentLangCode;
        private string workingDir;

        [OneTimeSetUp]
        public void Init() {
            this.workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [SetUp]
        public void Setup() {
            this.localization = new Localization(this.workingDir);
            this.localization.OnLocalizationChanged += code => this.currentLangCode = code;
        }

        [Test]
        public void SetupWithFallbacks_EventInvoked() {
            this.localization.SetupWithFallbacks();
            Assert.AreEqual("en", this.currentLangCode);
        }
    }
}
