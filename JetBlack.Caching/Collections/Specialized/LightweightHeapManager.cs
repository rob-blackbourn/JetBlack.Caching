using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Caching.Collections.Specialized
{
    public class LightweightHeapManager : IHeapManager
    {
        private readonly long _blockSize;
        private readonly IDictionary<Handle, Block> _allocatedBlocks = new Dictionary<Handle, Block>();
        private readonly ISet<Block> _freeBlocks = new HashSet<Block>();
        private long _length;

        public LightweightHeapManager(long blockSize = 2048)
        {
            _blockSize = blockSize;
        }

        public Handle Allocate(long length)
        {
            if (_freeBlocks.Count == 0)
                CreateFreeBlock(length);

            var block = FindFreeBlock(length);
            if (Equals(block, default(Block)))
                block = CreateFreeBlock(length);

            if (block.Length > length)
                block = Fragment(block, length);
            else
                _freeBlocks.Remove(block);

            _allocatedBlocks.Add(block.Handle, block);

            return block.Handle;
        }

        public void Free(Handle handle)
        {
            var block = GetAllocatedBlock(handle);
            _allocatedBlocks.Remove(handle);

            var previousAdjacentBlock = _freeBlocks.FirstOrDefault(x => x.Index + x.Length == block.Index);
            if (!Equals(previousAdjacentBlock, default(Block)))
            {
                _freeBlocks.Remove(previousAdjacentBlock);
                block = new Block(previousAdjacentBlock.Index, previousAdjacentBlock.Length + block.Length);
            }

            var endIndex = block.Index + block.Length;
            var followingAdjacentBlock = _freeBlocks.FirstOrDefault(x => x.Index == endIndex);
            if (!Equals(followingAdjacentBlock, default(Block)))
            {
                _freeBlocks.Remove(followingAdjacentBlock);
                block = new Block(block.Index, block.Length + followingAdjacentBlock.Length);
            }

            _freeBlocks.Add(block);
        }

        public Block CreateFreeBlock(long minimumLength)
        {
            var blocks = minimumLength / _blockSize;
            if (blocks * _blockSize < minimumLength)
                ++blocks;
            var length = blocks * _blockSize;
            var block = new Block(_length, length);
            _freeBlocks.Add(block);
            _length += length;
            return block;
        }

        public Block GetAllocatedBlock(Handle handle)
        {
            Block block;
            if (!_allocatedBlocks.TryGetValue(handle, out block))
                throw new Exception("invalid handle");
            return block;
        }

        public Block FindFreeBlock(long length)
        {
            var freeBlock = default(Block);
            foreach (var block in _freeBlocks.Where(x => x.Length >= length))
            {
                if (block.Length == length)
                    return block;

                if (Equals(freeBlock, default(Block)))
                    freeBlock = block;
                else if (block.Length < freeBlock.Length)
                    freeBlock = block;
            }

            return freeBlock;
        }

        public Block Fragment(Block freeBlock, long length)
        {
            if (freeBlock.Length < length) throw new ArgumentOutOfRangeException("length", "block too small");
            if (freeBlock.Length == length) return freeBlock;
            _freeBlocks.Remove(freeBlock);
            var block = new Block(freeBlock.Index + length, freeBlock.Length - length);
            _freeBlocks.Add(block);
            return new Block(freeBlock.Index, length);
        }
    }
}
