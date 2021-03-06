// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Environment
    {
        public enum SpecialFolderOption
        {
            None = 0,
            Create = SpecialFolderOptionValues.CSIDL_FLAG_CREATE,
            DoNotVerify = SpecialFolderOptionValues.CSIDL_FLAG_DONT_VERIFY,
        }

        // These values are specific to Windows and are known to SHGetFolderPath, however they are
        // also the values used in the SpecialFolderOption enum.  As such, we keep them as constants
        // with their Win32 names, but keep them here rather than in Interop.mincore as they're
        // used on all platforms.
        private static class SpecialFolderOptionValues
        {
            internal const int CSIDL_FLAG_CREATE = 0x8000; // force folder creation in SHGetFolderPath
            internal const int CSIDL_FLAG_DONT_VERIFY = 0x4000; // return an unverified folder path
        }
    }
}
