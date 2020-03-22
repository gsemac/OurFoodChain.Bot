using Microsoft.VisualStudio.TestTools.UnitTesting;
using OurFoodChain.Common.Utilities;

namespace OurFoodChain.Common.Tests.Utilities {

    [TestClass]
    public class SpeciesSizeMatchTests {

        [TestMethod]
        public void TestToStringWithFeet() {

            SpeciesSizeMatch match = SpeciesSizeMatch.Match("4.8 feet");

            Assert.AreEqual("**1.5 m** (4.8 ft)", match.ToString());

        }
        [TestMethod]
        public void TestToStringWithCentimeters() {

            SpeciesSizeMatch match = SpeciesSizeMatch.Match("15 cm");

            Assert.AreEqual("**15 cm** (5.9 in)", match.ToString());

        }

    }

}