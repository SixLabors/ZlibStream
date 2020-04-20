// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    /// <summary>
    /// Class that holds the contant values to zlib.
    /// </summary>
    public static class ZlibConst
    {
        /// <summary>
        /// Gets the version to zlib.net.
        /// </summary>
        /// <returns>The version string to this version of zlib.net.</returns>
        public static string Version() => typeof(ZlibConst).Assembly.GetName().Version.ToString(3);
    }
}
