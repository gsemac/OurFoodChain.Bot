using Microsoft.VisualStudio.TestTools.UnitTesting;
using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChainBotTests {

    [TestClass]
    public class BinomialNameTests {
        public BinomialNameTests() {
            //
            // TODO: Add constructor logic here
            //
        }

        [TestMethod]
        public void TestSpeciesOnly() {

            IBinomialName pair = BinomialName.Parse(string.Empty, "aspersum");

            Assert.AreEqual(pair.ToString(), "aspersum");

        }
        [TestMethod]
        public void TestFullGenusAndSpecies() {

            IBinomialName pair = BinomialName.Parse("cornu", "aspersum");

            Assert.AreEqual(pair.ToString(), "Cornu aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusAndSpecies() {

            IBinomialName pair = BinomialName.Parse("c.", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusWithoutPeriodAndSpecies() {

            IBinomialName pair = BinomialName.Parse("c", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpecies() {

            IBinomialName pair = BinomialName.Parse(string.Empty, "c.aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpeciesWithBeginningPeriod() {

            IBinomialName pair = BinomialName.Parse(string.Empty, "c.aspersum.");

            Assert.AreEqual(pair.ToString(), "C. aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningPeriod() {

            IBinomialName pair = BinomialName.Parse(string.Empty, ".aspersum");

            Assert.AreEqual(pair.ToString(), ".aspersum");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithTrailingPeriod() {

            IBinomialName pair = BinomialName.Parse(string.Empty, "aspersum.");

            Assert.AreEqual(pair.ToString(), "aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningAndTrailingPeriod() {

            IBinomialName pair = BinomialName.Parse(string.Empty, ".aspersum.");

            Assert.AreEqual(pair.ToString(), ".aspersum.");

        }

    }
}