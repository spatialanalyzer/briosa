using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Briosa.Server.Services;

// Matches the installer's reviewed WindowsSdkRegistrationEnvironment ACL policy.
internal static class InstallationProtection
{
    [SupportedOSPlatform("windows")]
    internal static bool Allows(FileSystemSecurity security, bool includeWrite)
    {
        if (new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0).DiscretionaryAcl is null)
            return false;
        static bool Trusted(string sid) => sid is "S-1-5-32-544" or "S-1-5-18" or
            "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
        if (security.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner || !Trusted(owner.Value))
            return false;
        var changes = FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles |
            FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;
        if (includeWrite) changes |= FileSystemRights.Write;
        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType != AccessControlType.Allow ||
                (rule.PropagationFlags & PropagationFlags.InheritOnly) != 0) continue;
            var genericWrite = (unchecked((uint)rule.FileSystemRights) & 0x50000000) != 0;
            if (((rule.FileSystemRights & changes) != 0 || genericWrite) && !Trusted(rule.IdentityReference.Value))
                return false;
        }
        return true;
    }
}
