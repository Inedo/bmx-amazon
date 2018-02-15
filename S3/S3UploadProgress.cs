using System;

namespace Inedo.BuildMasterExtensions.Amazon.S3
{
    internal sealed class S3UploadProgress : IEquatable<S3UploadProgress>
    {
        public S3UploadProgress(int percent)
        {
            this.Percent = percent;
        }

        public int Percent { get; private set; }

        public bool Equals(S3UploadProgress other)
        {
            if (object.ReferenceEquals(other, null))
                return false;

            if (object.ReferenceEquals(this, other))
                return true;

            return this.Percent == other.Percent;
        }
        public override bool Equals(object obj)
        {
            return this.Equals(obj as S3UploadProgress);
        }
        public override int GetHashCode()
        {
            return this.Percent;
        }
    }
}
