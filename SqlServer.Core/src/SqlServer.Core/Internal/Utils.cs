using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    internal static class Utils
    {
        public static bool ObfuscatePassword(this Span<byte> self, string password, out Span<byte> output)
        {
            var passwordSpan = MemoryMarshal.Cast<char,byte>(password.AsSpan());

            if (self.Length < passwordSpan.Length)
            {
                output = default;
                return false;
            }

            output = self.Slice(0, passwordSpan.Length);
            for (var i = 0; i < passwordSpan.Length; i++)
            {
                var b = passwordSpan[i];
                b = (byte)((b >> 4) | (b << 4));
                b ^= 0xA5;
                output[i] = b;
            }

            return true;
        }
    }
}
