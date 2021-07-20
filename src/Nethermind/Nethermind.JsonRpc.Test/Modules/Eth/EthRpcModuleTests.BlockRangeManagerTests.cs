//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
// 

using FluentAssertions;
using Nethermind.Blockchain.Find;
using Nethermind.Core;
using Nethermind.Core.Test.Builders;
using Nethermind.JsonRpc.Modules.Eth;
using NSubstitute;
using NUnit.Framework;
using static Nethermind.JsonRpc.Modules.Eth.EthRpcModule.LastBlockNumberConsts;

namespace Nethermind.JsonRpc.Test.Modules.Eth
{
    public partial class EthRpcModuleTests
    {
        [TestFixture]
        public class BlockRangeManagerTests
        {
            [TestCase(2,2,1)]
            [TestCase(7,7,6)]
            [TestCase(32,32,31)]
            public void
                ResolveBlockRange_IfLastBlockIsPendingBlockAndPendingBlockExists_LastBlockNumberSetToPendingBlockNumber(long blockNumber, long lastBlockExpected, long headNumberExpected)
            {
                long lastBlockNumber = PendingBlockNumber;
                long blockCount = 1;
                long? headBlockNumber = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindPendingBlock().Returns((Block) Build.A.Block.WithNumber(blockNumber).TestObject);
                BlockRangeManager blockRangeManager = new(blockFinder);

                blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumber);

                lastBlockNumber.Should().Be(lastBlockExpected);
            }
            
            [Test]
            public void
                ResolveBlockRange_IfLastBlockNumberIsPendingBlockNumberAndPendingBlockIsNull_LastBlockNumberSetToLatestBlockNumberMode()
            {
                long lastBlockNumber = PendingBlockNumber;
                long blockCount = 1;
                long? headBlockNumber = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindPendingBlock().Returns((Block) null);
                BlockRangeManager blockRangeManager = new(blockFinder);

                blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumber);

                lastBlockNumber.Should().Be(LatestBlockNumber);
            }

            [Test]
            public void
                ResolveBlockRange_IfLastBlockIsPendingBlockAndPendingBlockIsNullAndBlockCountEquals1_ErrorReturned()
            {
                long lastBlockNumber = PendingBlockNumber;
                long blockCount = 1;
                long? headBlockNumber = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindPendingBlock().Returns((Block) null);
                BlockRangeManager blockRangeManager = new(blockFinder);

                ResultWrapper<BlockRangeInfo> resultWrapper = blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumber);

                ResultWrapper<BlockRangeInfo> expected =
                    ResultWrapper<BlockRangeInfo>.Fail("Invalid pending block reduced blockCount to 0.");
                
                resultWrapper.Result.Error.Should().Be("Invalid pending block reduced blockCount to 0.");
                resultWrapper.Result.ResultType.Should().Be(ResultType.Failure);
            }
            
            [TestCase(2)]
            [TestCase(7)]
            [TestCase(32)]
            public void ResolveBlockRange_IfLastBlockIsNotPendingBlockAndHeadBlockNumberIsNull_ErrorReturned(long lastBlockNumber)
            {
                long blockCount = 1;
                long? headBlockNumber = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindHeadBlock().Returns((Block) null);
                BlockRangeManager blockRangeManager = new(blockFinder);

                ResultWrapper<BlockRangeInfo> resultWrapper = blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumber);

                ResultWrapper<BlockRangeInfo> expected =
                    ResultWrapper<BlockRangeInfo>.Fail("Invalid pending block reduced blockCount to 0.");
                
                resultWrapper.Result.Error.Should().Be("Head block not found.");
                resultWrapper.Result.ResultType.Should().Be(ResultType.Failure);
            }

            [Test]
            public void
                ResolveBlockRange_IfLastBlockIsPendingBlockAndPendingBlockIsNull_ErrorReturned()
            {
                long lastBlockNumber = PendingBlockNumber;
                long blockCount = 1;
                long? headBlockNumber = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindPendingBlock().Returns((Block) null);
                BlockRangeManager blockRangeManager = new(blockFinder);

                ResultWrapper<BlockRangeInfo> resultWrapper = blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumber);

                ResultWrapper<BlockRangeInfo> expected =
                    ResultWrapper<BlockRangeInfo>.Fail("Invalid pending block reduced blockCount to 0.");
                
                resultWrapper.Result.Error.Should().Be("Invalid pending block reduced blockCount to 0.");
                resultWrapper.Result.ResultType.Should().Be(ResultType.Failure);
            }
            
            [TestCase(2,2)]
            [TestCase(7,7)]
            [TestCase(32,32)]
            public void ResolveBlockRange_IfLastBlockIsEqualToLatestBlockNumber_SetLastBlockToHeadBlockNumber(long headBlockNumber, long expected)
            {
                long lastBlockNumber = LatestBlockNumber;
                long blockCount = 1;
                long? headBlockNumberVar = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                BlockRangeInfo blockRangeInfo = new();
                blockFinder.FindHeadBlock().Returns(Build.A.Block.WithNumber(headBlockNumber).TestObject);
                BlockRangeManager blockRangeManager = new(blockFinder);

                blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumberVar);

                lastBlockNumber.Should().Be(expected);
            }

            [TestCase(3,5)]
            [TestCase(4,10)]
            [TestCase(0,1)]
            public void
                ResolveBlockRange_IfPendingBlockDoesNotExistAndLastBlockNumberGreaterThanHeadNumber_ReturnsError(long headBlockNumber, long lastBlockNumber)
            {
                long blockCount = 1;
                long? headBlockNumberVar = null;
                IBlockFinder blockFinder = Substitute.For<IBlockFinder>();
                blockFinder.FindPendingBlock().Returns((Block) null);
                blockFinder.FindHeadBlock().Returns(Build.A.Block.WithNumber(headBlockNumber).TestObject);
                BlockRangeManager blockRangeManager = new(blockFinder);

                ResultWrapper<BlockRangeInfo> resultWrapper = blockRangeManager.ResolveBlockRange(ref lastBlockNumber, ref blockCount, 1, ref headBlockNumberVar);

                resultWrapper.Result.Error.Should().Be("Pending block not present and last block number greater than head number.");
                resultWrapper.Result.ResultType.Should().Be(ResultType.Failure);
            }

            [Test]
            public void ResolveBlockRange_IfMaxHistoryIsNot0_TooOldCountCalculatedCorrectly()
            {
                
            }

            [Test]
            public void
                ResolveBlockRange_IfBlockCountMoreThanBlocksUptoLastBlockNumber_BlockCountSetToBlocksUptoLastBlockNumber()
            {
                
            }

            public class TestableBlockRangeManager : BlockRangeManager
            {
                public TestableBlockRangeManager(IBlockFinder blockFinder) : base(blockFinder)
                {
                }

            }
        }
    }
}
