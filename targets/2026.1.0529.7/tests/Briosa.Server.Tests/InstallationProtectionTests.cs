using System.Security.AccessControl;
using Briosa.Server.Services;

namespace Briosa.Server.Tests;

public sealed class InstallationProtectionTests
{
    [Theory]
    [InlineData("O:SYG:SY", true, false)]
    [InlineData("O:SYG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)", true, true)]
    [InlineData("O:SYG:SYD:(A;;FA;;;SY)(A;;FW;;;BU)", false, true)]
    [InlineData("O:SYG:SYD:(A;;FA;;;SY)(A;;FW;;;BU)", true, false)]
    [InlineData("O:SYG:SYD:(A;;FA;;;SY)(A;;0x40;;;BU)", false, false)]
    [InlineData("O:SYG:SYD:(A;;GA;;;BU)", false, false)]
    [InlineData("O:SYG:SYD:(A;CIIO;GA;;;BU)", true, true)]
    [InlineData("O:S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464G:SYD:(A;;FA;;;SY)", true, true)]
    [InlineData("O:BUG:SYD:(A;;FA;;;SY)", true, false)]
    public void WindowsAclDistinguishesPayloadFromAncestors(string sddl, bool includeWrite, bool expected)
    {
        if (!OperatingSystem.IsWindows()) return;
        var security = new DirectorySecurity();
        security.SetSecurityDescriptorSddlForm(sddl);
        Assert.Equal(expected, InstallationProtection.Allows(security, includeWrite));
    }
}
