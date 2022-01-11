﻿//  Copyright (c) 2021 Demerzel Solutions Limited
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

using System;
using System.IO;
using Nethermind.Blockchain;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Core.Test.IO;
using Nethermind.Crypto;
using Nethermind.Db;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Serialization.Json;
using Nethermind.Specs;
using Nethermind.Specs.ChainSpecStyle;
using Nethermind.Specs.Forks;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Merge.Plugin.Test
{
    public class PoSSwitcherTests
    {
        [Test]
        [Ignore("Need to ensure that transition process is correct")]
        public void Correctly_validate_headers_with_TTD()
        {
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = CreatePosSwitcher(200, blockTree);

            BlockHeader blockHeader = Build.A.BlockHeader.WithTotalDifficulty(100L).WithNumber(1).TestObject;
            BlockHeader blockHeader2 = Build.A.BlockHeader.WithTotalDifficulty(200L).WithNumber(2).TestObject;
            BlockHeader blockHeader3 = Build.A.BlockHeader.WithTotalDifficulty(300L).WithNumber(3).TestObject;

            Assert.AreEqual(false, poSSwitcher.IsPos(blockHeader));
            Assert.AreEqual(true, poSSwitcher.IsPos(blockHeader2));
            Assert.AreEqual(true, poSSwitcher.IsPos(blockHeader3));
        }

        [Test]
        [Ignore("Need to ensure that transition process is correct")]
        public void Switch_with_terminal_hash()
        {
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = CreatePosSwitcher(1000000000000000, blockTree);

            Block firstBlock = Build.A.Block.WithParentHash(Keccak.Compute("test2")).WithTotalDifficulty(100L).WithNumber(1).TestObject;
            Block secondBlock = Build.A.Block.WithParentHash(Keccak.Compute("test1")).WithTotalDifficulty(200L).WithNumber(2).TestObject;
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(firstBlock));
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(secondBlock));

            Assert.AreEqual(false, poSSwitcher.IsPos(firstBlock.Header));
            Assert.AreEqual(true, poSSwitcher.IsPos(secondBlock.Header));
        }

        [Test]
        [Ignore("Need to ensure that transition process is correct")]
        public void Is_pos_without_switch_return_expected_results()
        {
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = CreatePosSwitcher(200, blockTree);
            Block firstBlock = Build.A.Block.WithTotalDifficulty(100L).WithNumber(1).TestObject;
            Block secondBlock = Build.A.Block.WithTotalDifficulty(200L).WithNumber(2).TestObject;
            Block thirdBlock = Build.A.Block.WithTotalDifficulty(400L).WithNumber(3).TestObject;

            Assert.AreEqual(false, poSSwitcher.IsPos(firstBlock.Header));
            Assert.AreEqual(true, poSSwitcher.IsPos(secondBlock.Header));
            Assert.AreEqual(true, poSSwitcher.IsPos(thirdBlock.Header));
        }

        [Test]
        [Ignore("Need to ensure that transition process is correct")]
        public void Is_pos__with_switch_return_expected_results()
        {
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = CreatePosSwitcher(200, blockTree);
            Block firstBlock = Build.A.Block.WithTotalDifficulty(100L).WithNumber(1).TestObject;
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(firstBlock));
            Block secondBlock = Build.A.Block.WithTotalDifficulty(200L).WithNumber(2).TestObject;
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(secondBlock));
            Block thirdBlock = Build.A.Block.WithTotalDifficulty(400L).WithNumber(3).TestObject;
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(thirdBlock));

            Assert.AreEqual(false, poSSwitcher.IsPos(firstBlock.Header));
            Assert.AreEqual(true, poSSwitcher.IsPos(secondBlock.Header));
            Assert.AreEqual(true, poSSwitcher.IsPos(thirdBlock.Header));
        }
        
        [Test]
        public void Initial_TTD_should_be_null()
        {
            UInt256? expectedTtd = null;
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = new(new MergeConfig(), new MemDb(), blockTree, MainnetSpecProvider.Instance, new ChainSpec(), LimboLogs.Instance);

            Assert.AreEqual(expectedTtd, poSSwitcher.TerminalTotalDifficulty);
        }
        
        [Test]
        public void Override_TTD_and_number_from_merge_config()
        {
            UInt256 expectedTtd = 340;
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            TestSpecProvider specProvider = new(London.Instance);
            PoSSwitcher poSSwitcher = new(new MergeConfig() {TerminalTotalDifficulty = "340", TerminalBlockNumber = 2000}, new MemDb(), blockTree, specProvider, new ChainSpec() {TerminalTotalDifficulty = 500, TerminalPoWBlockNumber = 1000}, LimboLogs.Instance);

            Assert.AreEqual(expectedTtd, poSSwitcher.TerminalTotalDifficulty);
            Assert.AreEqual(2001, specProvider.MergeBlockNumber);
        }
        
        [Test]
        public void Read_TTD_from_chainspec_if_not_specified_in_merge_config()
        {
            UInt256 expectedTtd = 10;
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            ChainSpecLoader loader = new(new EthereumJsonSerializer());
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Specs/test_spec.json");
            ChainSpec chainSpec = loader.Load(File.ReadAllText(path));

            ChainSpecBasedSpecProvider specProvider = new(chainSpec);
            PoSSwitcher poSSwitcher = new(new MergeConfig(), new MemDb(), blockTree, specProvider, chainSpec, LimboLogs.Instance);

            Assert.AreEqual(expectedTtd, poSSwitcher.TerminalTotalDifficulty);
            Assert.AreEqual(101, specProvider.MergeBlockNumber);
        }

        [Test]
        public void Switch_when_TTD_is_reached()
        {
            IBlockTree blockTree = Substitute.For<IBlockTree>();
            PoSSwitcher poSSwitcher = CreatePosSwitcher(200, blockTree);

            Assert.AreEqual(false, poSSwitcher.HasEverReachedTerminalPoWBlock());
            Block block = Build.A.Block.WithTotalDifficulty(300L).WithNumber(1).TestObject;
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(block));

            Assert.AreEqual(true, poSSwitcher.HasEverReachedTerminalPoWBlock());
        }
        
        [Test]
        public void Can_load_parameters_after_the_restart()
        {
            using MemDb metadataDb = new();
            
            UInt256 configTerminalTotalDifficulty = 10L;
            long terminalBlock = 15_773_000;
            
            IBlockTree? blockTree = Substitute.For<IBlockTree>();
            ISpecProvider specProvider = new TestSpecProvider(Berlin.Instance);
            PoSSwitcher poSSwitcher = CreatePosSwitcher(configTerminalTotalDifficulty, blockTree, metadataDb, specProvider);
            Block block = Build.A.Block.WithTotalDifficulty(300L).WithNumber(terminalBlock).TestObject;
            block.Header.Hash = block.CalculateHash();
            blockTree.NewHeadBlock += Raise.Event<EventHandler<BlockEventArgs>>(new BlockEventArgs(block));
            Assert.AreEqual(terminalBlock + 1, specProvider.MergeBlockNumber);
            Assert.AreEqual(true, poSSwitcher.HasEverReachedTerminalPoWBlock());

            TestSpecProvider newSpecProvider = new(London.Instance);
            // we're using the same mem db for new Switcher
            PoSSwitcher newPoSSwitcher = CreatePosSwitcher(configTerminalTotalDifficulty, blockTree, metadataDb, newSpecProvider);
            
            Assert.AreEqual(terminalBlock + 1, newSpecProvider.MergeBlockNumber);
            Assert.AreEqual(true, newPoSSwitcher.HasEverReachedTerminalPoWBlock());
        }

        private static PoSSwitcher CreatePosSwitcher(UInt256 terminalTotalDifficulty, IBlockTree blockTree, IDb? db = null, ISpecProvider? specProvider = null)
        {
            db ??= new MemDb();
            MergeConfig? mergeConfig = new() {Enabled = true};
            return new PoSSwitcher(mergeConfig, db, blockTree, specProvider ?? MainnetSpecProvider.Instance, new ChainSpec() { TerminalTotalDifficulty = terminalTotalDifficulty}, LimboLogs.Instance);
        }
    }
}
