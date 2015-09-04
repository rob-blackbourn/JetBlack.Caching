using System;
using System.IO;

namespace JetBlack.Caching.Collections.Specialized
{
    public class FileStreamHeap : StreamHeap
    {
        private readonly bool _isFileOwner;

        public FileStreamHeap(FileStream stream, IHeapManager heapManager)
            : this(stream, false, false, heapManager)
        {
        }

        public FileStreamHeap(Func<FileStream> fileStreamFactory, IHeapManager heapManager)
            : this(fileStreamFactory(), true, true, heapManager)
        {
        }

        protected FileStreamHeap(FileStream stream, bool isStreamOwner, bool isFileOwner, IHeapManager heapManager)
            : base(stream, isStreamOwner, heapManager)
        {
            _isFileOwner = isFileOwner;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_isFileOwner)
                File.Delete(((FileStream)Stream).Name);
        }
    }
}
