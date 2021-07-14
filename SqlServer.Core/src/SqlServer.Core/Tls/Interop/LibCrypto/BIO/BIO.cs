using System;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        public struct BIO
        {
            private IntPtr _pointer;

            public void Free()
            {
                if (_pointer != IntPtr.Zero)
                {
                    BIO_free(this);
                    _pointer = IntPtr.Zero;
                }
            }

            public override bool Equals(object obj)
            {
                return obj is BIO bio ? this == bio : false;
            }

            public override int GetHashCode() => _pointer.GetHashCode();

            public static bool operator ==(BIO left, BIO right) => left._pointer == right._pointer;

            public static bool operator !=(BIO left, BIO right) => !(left == right);
        }
    }
}
