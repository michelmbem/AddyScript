using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddyScript.Runtime.Utilities;

namespace AddyScript.Test
{
    [TestClass]
    public class PackFormatTest
    {
        [TestMethod]
        public void ParseTest()
        {
            var format = PackFormat.Parse("<hI12s");
            Assert.AreEqual(format.Endianness, Endianness.LittleEndian);
            Assert.AreEqual(format.Items.Count, 3);
            Assert.AreEqual(format.Length, 3);
            Assert.AreEqual(format.Items[0].Type, PackFormatType.Short);
            Assert.AreEqual(format.Items[1].Type, PackFormatType.UInteger);
            Assert.AreEqual(format.Items[2].Type, PackFormatType.CString);
            Assert.AreEqual(format.Items[2].Count, 12);
        }
    }
}
