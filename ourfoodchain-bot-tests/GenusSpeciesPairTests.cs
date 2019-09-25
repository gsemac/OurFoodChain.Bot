using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChainBotTests {

    [TestClass]
    public class GenusSpeciesPairTests {
        public GenusSpeciesPairTests() {
            //
            // TODO: Add constructor logic here
            //
        }

        [TestMethod]
        public void TestSpeciesOnly() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, "aspersum");

            Assert.AreEqual(pair.ToString(), "aspersum");

        }
        [TestMethod]
        public void TestFullGenusAndSpecies() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse("cornu", "aspersum");

            Assert.AreEqual(pair.ToString(), "Cornu aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusAndSpecies() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse("c.", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusWithoutPeriodAndSpecies() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse("c", "aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpecies() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, "c.aspersum");

            Assert.AreEqual(pair.ToString(), "C. aspersum");

        }
        [TestMethod]
        public void TestAbbrieviatedGenusCombinedWithSpeciesWithBeginningPeriod() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, "c.aspersum.");

            Assert.AreEqual(pair.ToString(), "C. aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningPeriod() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, ".aspersum");

            Assert.AreEqual(pair.ToString(), ".aspersum");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithTrailingPeriod() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, "aspersum.");

            Assert.AreEqual(pair.ToString(), "aspersum.");

        }
        [TestMethod]
        public void TestSpeciesOnlyWithBeginningAndTrailingPeriod() {

            OurFoodChain.GenusSpeciesPair pair = OurFoodChain.GenusSpeciesPair.Parse(string.Empty, ".aspersum.");

            Assert.AreEqual(pair.ToString(), ".aspersum.");

        }

    }
}