﻿using NSubstitute;
using SmarterConstruction.Pathfinding;
using System.Collections.Generic;
using Verse;
using Xunit;

namespace SmarterConstructionSpec
{
    public class ClosedRegionDetectorTests
    {

        [Fact]
        public void ClosedRegionCreatedByAddingImpassable_NoClosedRegion()
        {
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(1, 0, -1),
                new IntVec3(2, 0, 0)
            });

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, IntVec3.Zero);

            Assert.Empty(result);
        }

        [Fact]
        public void ClosedRegionCreatedByAddingImpassable_NoPassableTiles()
        {
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(-1, 0, 0),
                new IntVec3(0, 0, -1),
                new IntVec3(1, 0, 0),
                new IntVec3(0, 0, 1)
            });

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, IntVec3.Zero);

            Assert.Empty(result);
        }

        [Fact]
        public void ClosedRegionCreatedByAddingImpassable_SingleClosedRegion()
        {
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(1, 0, -1),
                new IntVec3(2, 0, 0),
                new IntVec3(1, 0, 1)
            });

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, IntVec3.Zero);

            Assert.Single(result);
            Assert.All(result, p => Assert.Equal(new IntVec3(1, 0, 0), p));
        }

        [Fact]
        public void ClosedRegionCreatedByAddingImpassable_TwoClosedRegions()
        {
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(-2, 0, -1),
                new IntVec3(-1, 0, -1),
                //new IntVec3(0, 0, -1), // Left open to create an open region as well
                new IntVec3(1, 0, -1),
                new IntVec3(2, 0, -1),

                new IntVec3(2, 0, 0),

                new IntVec3(2, 0, 1),
                new IntVec3(1, 0, 1),
                new IntVec3(0, 0, 1),
                new IntVec3(-1, 0, 1),
                new IntVec3(-2, 0, 1),

                new IntVec3(-2, 0, 0)
            });

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, IntVec3.Zero);

            Assert.Equal(2, result.Count);
            Assert.Single(result, pos => pos == new IntVec3(-1, 0, 0));
            Assert.Single(result, pos => pos == new IntVec3(1, 0, 0));
        }

        private IPathGrid CreateMockWithImpassableTiles(List<IntVec3> impassableTiles)
        {
            var mock = Substitute.For<IPathGrid>();
            mock.Walkable(default).ReturnsForAnyArgs(true);
            foreach(var tile in impassableTiles)
            {
                mock.Walkable(tile).Returns(false);
            }
            return mock;
        }
    }
}
