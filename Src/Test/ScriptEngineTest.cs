using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AddyScript.Test
{
    /// <summary>
    ///This is a test class for ScriptEngineTest and is intended
    ///to contain all ScriptEngineTest Unit Tests
    ///</summary>
    [TestClass]
    public class ScriptEngineTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        delegate int SumType(int a, int b);
        
        /// <summary>
        ///A test for GetDelegate
        ///</summary>
        [TestMethod]
        public void GetDelegateTest()
        {
            var ctx = new ScriptContext();
            var engine = new ScriptEngine(ctx);
            engine.Execute("function sum(a, b) {return a + b;}");

            var sum = (SumType) engine.GetDelegate("sum", typeof(SumType));
            var x = sum(7, -3);

            Assert.AreEqual(x, 4);
        }

        /// <summary>
        ///A test for Invoke
        ///</summary>
        [TestMethod]
        public void InvokeTest()
        {
            var ctx = new ScriptContext();
            var engine = new ScriptEngine(ctx);
            engine.Execute("function sum(a, b) {return a + b;}");

            var x = engine.Invoke("sum", 10, 5).AsInt32;

            Assert.AreEqual(x, 15);
        }
    }
}
