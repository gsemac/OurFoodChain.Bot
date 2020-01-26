using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, "aspersum");

            Assert.AreEqual(pair.ToString(), "aspersum");

        }
        [TestMethod]
        public void TestFullGenusAndSpecies() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse("cornu", "aspersum");

            Assert.AreEqual(pair.ToString(), "Cornu aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusAndSpecies() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse("c.", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusWithoutPeriodAndSpecies() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse("c", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpecies() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, "c.aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpeciesWithBeginningPeriod() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, "c.aspersum.");

            Assert.AreEqual(pair.ToString(), "C. aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningPeriod() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, ".aspersum");

            Assert.AreEqual(pair.ToString(), ".aspersum");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithTrailingPeriod() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, "aspersum.");

            Assert.AreEqual(pair.ToString(), "aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningAndTrailingPeriod() {

            OurFoodChain.BinomialName pair = OurFoodChain.BinomialName.Parse(string.Empty, ".aspersum.");

            Assert.AreEqual(pair.ToString(), ".aspersum.");

        }

    }
}