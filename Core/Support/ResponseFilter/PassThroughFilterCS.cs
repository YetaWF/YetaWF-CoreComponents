/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;

namespace YetaWF.Core.ResponseFilter {
    public class PassThroughFilter : Stream {
        private readonly Stream _originalFilter;
        public PassThroughFilter(Stream originalFilter) {
            _originalFilter = originalFilter;
        }
        public override void Flush() { _originalFilter.Flush(); }
        public override long Seek(long offset, SeekOrigin origin) { return _originalFilter.Seek(offset, origin); }
        public override void SetLength(long value) { _originalFilter.SetLength(value); }
        public override int Read(byte[] buffer, int offset, int count) { return _originalFilter.Read(buffer, offset, count); }
        public override void Write(byte[] buffer, int offset, int count) { _originalFilter.Write(buffer, offset, count); }
        public override bool CanRead { get { return _originalFilter.CanRead; } }
        public override bool CanSeek { get { return _originalFilter.CanSeek; } }
        public override bool CanWrite { get { return _originalFilter.CanWrite; } }
        public override long Length { get { return _originalFilter.Length; } }
        public override long Position { get { return _originalFilter.Position; } set { _originalFilter.Position = value; } }
    }
}