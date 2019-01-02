using AddyScript.Runtime.NativeTypes;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AddyScript.Test
{
    /// <summary>
    ///This is a test class for BigDecimalTest and is intended
    ///to contain all BigDecimalTest Unit Tests
    ///</summary>
    [TestClass]
    public class BigDecimalTest
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

        /// <summary>
        ///A test for op_Division
        ///</summary>
        [TestMethod]
        public void op_DivisionTest()
        {
            BigDecimal a = new BigDecimal("987654321098765432109876.54321");
            BigDecimal b = new BigDecimal("1000000000000");
            BigDecimal expected = new BigDecimal("987654321098.76543210987654321");
            BigDecimal actual = (a / b);
            Assert.AreEqual(expected, actual);
        }
    }
}
