using FluentAssertions;
using NSubstitute;
using SmarterConstruction.Core;
using System.Collections.Generic;
using System.Linq;
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

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, new HashSet<IntVec3> { IntVec3.Zero });

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

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, new HashSet<IntVec3> { IntVec3.Zero });

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

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, new HashSet<IntVec3> { IntVec3.Zero });

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

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, new HashSet<IntVec3> { IntVec3.Zero });

            Assert.Equal(2, result.Count);
            Assert.Single(result, pos => pos == new IntVec3(-1, 0, 0));
            Assert.Single(result, pos => pos == new IntVec3(1, 0, 0));
        }

        [Fact]
        public void ClosedRegionCreatedByAddingImpassable_MultipleBlockedTiles()
        {
            /*
             *  2 ###
             *  1 #
             *  0 #
             * -1 .#
             */
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(2, 0, 2),
                new IntVec3(1, 0, 2),
                new IntVec3(0, 0, 2),
                new IntVec3(0, 0, 1),
                new IntVec3(0, 0, 0),
                new IntVec3(1, 0, -1)
            });
            /*
             *  1 ..#
             *  0 ..#
             */
            var blockers = new HashSet<IntVec3>
            {
                new IntVec3(2, 0, 1),
                new IntVec3(2, 0, 0)
            };

            var result = ClosedRegionDetector.ClosedRegionCreatedByAddingImpassable(mock, blockers);

            Assert.Equal(2, result.Count);
            Assert.Single(result, pos => pos == new IntVec3(1, 0, 1));
            Assert.Single(result, pos => pos == new IntVec3(1, 0, 0));
        }

        [Fact]
        public void FindSafeConstructionSpots_SingleBlockerOneUnwalkable()
        {
            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(1, 0, 0)
            });
            var expected = new HashSet<IntVec3>
            {
                new IntVec3(-1, 0, 1),
                new IntVec3(0, 0, 1),
                new IntVec3(1, 0, 1),
                //new IntVec3(1, 0, 0), // the only blocked tile
                new IntVec3(1, 0, -1),
                new IntVec3(0, 0, -1),
                new IntVec3(-1, 0, -1),
                new IntVec3(-1, 0, 0)
            };

            var result = ClosedRegionDetector.FindSafeConstructionSpots(mock, new HashSet<IntVec3> { IntVec3.Zero });

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void FindSafeConstructionSpots_SingleBlockerAllUnwalkable()
        {
            var mock = Substitute.For<IPathGrid>();
            mock.Walkable(default).ReturnsForAnyArgs(false);

            var result = ClosedRegionDetector.FindSafeConstructionSpots(mock, new HashSet<IntVec3> { IntVec3.Zero });

            result.Should().HaveCount(0);
        }

        [Fact]
        public void FindSafeConstructionSpots_MultipleBlockersSeveralUnwalkable()
        {
            /* . walkable tile
            *  ! unwalkable tile
            *  # construction tile
            *  
            *  ...!
            *  .##!
            *  .##.
            *  .!!.
            *  ^ (0,0)
            */

            var mock = CreateMockWithImpassableTiles(new List<IntVec3>
            {
                new IntVec3(1, 0, 0),
                new IntVec3(2, 0, 0),
                new IntVec3(3, 0, 3),
                new IntVec3(3, 0, 2)
            });
            var constructionTiles = new HashSet<IntVec3>
            {
                new IntVec3(1, 0, 1),
                new IntVec3(2, 0, 1),
                new IntVec3(1, 0, 2),
                new IntVec3(2, 0, 2)
            };
            var expected = new HashSet<IntVec3>
            {
                new IntVec3(0, 0, 0),
                new IntVec3(0, 0, 1),
                new IntVec3(0, 0, 2),
                new IntVec3(0, 0, 3),
                new IntVec3(1, 0, 3),
                new IntVec3(2, 0, 3),
                new IntVec3(3, 0, 0),
                new IntVec3(3, 0, 1)
            };

            var result = ClosedRegionDetector.FindSafeConstructionSpots(mock, constructionTiles);

            result.Should().BeEquivalentTo(expected);
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
