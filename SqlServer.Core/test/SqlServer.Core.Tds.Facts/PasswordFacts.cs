using System;
using SqlServer.Core.Internal;
using Xunit;

namespace SqlServer.Core.Tds.Facts
{
    public class PasswordFacts
    {
        [Fact]
        public void CheckPasswordAgainstKnownGood()
        {
            var password = "testpassword";
            var p = new byte[] { 0xe2, 0xa5, 0xf3, 0xa5, 0x92, 0xa5, 0xe2, 0xa5, 0xa2, 0xa5, 0xb3, 0xa5, 0x92, 0xa5, 0x92, 0xa5, 0xd2, 0xa5, 0x53, 0xa5, 0x82, 0xa5, 0xe3, 0xa5 };
            var spanSize = new byte[password.Length * 2];

            Assert.True(Utils.ObfuscatePassword(spanSize, password, out var result));

            for (var i = 0; i < p.Length; i++)
            {
                Assert.Equal(p[i], result[i]);
            }
        }
    }
}
