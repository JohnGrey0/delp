namespace Delp.Core.Tools.DevUtilities;

public static partial class GitignoreData
{
    private static readonly IReadOnlyList<GitignoreTemplate> OsTemplates =
    [
        new("Windows", "OS",
            """
            # Windows thumbnail cache files
            Thumbs.db
            Thumbs.db:encryptable
            ehthumbs.db
            ehthumbs_vista.db
            
            # Dump file
            *.stackdump
            
            # Folder config file
            [Dd]esktop.ini
            
            # Recycle Bin used on file shares
            $RECYCLE.BIN/
            
            # Windows Installer files
            *.cab
            *.msi
            *.msix
            *.msm
            *.msp
            
            # Windows shortcuts
            *.lnk
            """
        ),
        new("macOS", "OS",
            "# General\n" +
            ".DS_Store\n" +
            ".localized\n" +
            "__MACOSX/\n" +
            ".AppleDouble\n" +
            ".LSOverride\n" +
            "Icon[\r]\n" +
            "\n" +
            "# Resource forks\n" +
            "._*\n" +
            "\n" +
            "# Files and directories that might appear in the root of a volume\n" +
            ".DocumentRevisions-V100\n" +
            ".fseventsd\n" +
            ".Spotlight-V100\n" +
            ".TemporaryItems\n" +
            ".Trashes\n" +
            ".VolumeIcon.icns\n" +
            ".com.apple.timemachine.donotpresent\n" +
            ".com.apple.timemachine.supported\n" +
            ".PKInstallSandboxManager\n" +
            ".PKInstallSandboxManager-SystemSoftware\n" +
            ".hotfiles.btree\n" +
            ".vol\n" +
            ".file\n" +
            ".disk_label*\n" +
            "lost+found\n" +
            ".HFS+ Private Directory Data[\r]\n" +
            "\n" +
            "# Directories potentially created on remote AFP share\n" +
            ".AppleDB\n" +
            ".AppleDesktop\n" +
            "Network Trash Folder\n" +
            "Temporary Items\n" +
            ".apdisk\n" +
            "\n" +
            "# Mac OS 6 to 9\n" +
            "Desktop DB\n" +
            "Desktop DF\n" +
            "TheFindByContentFolder\n" +
            "TheVolumeSettingsFolder\n" +
            ".FBCIndex\n" +
            ".FBCSemaphoreFile\n" +
            ".FBCLockFolder\n" +
            "\n" +
            "# Quota system\n" +
            ".quota.group\n" +
            ".quota.user\n" +
            ".quota.ops.group\n" +
            ".quota.ops.user\n" +
            "\n" +
            "# TimeMachine\n" +
            "Backups.backupdb\n" +
            ".MobileBackups\n" +
            ".MobileBackups.trash\n" +
            "MobileBackups.trash\n" +
            "tmbootpicker.efi"
        ),
        new("Linux", "OS",
            """
            *~
            
            # temporary files which can be created if a process still has a handle open of a deleted file
            .fuse_hidden*
            
            # Metadata left by Dolphin file manager, which comes with KDE Plasma
            .directory
            
            # Linux trash folder which might appear on any partition or disk
            .Trash-*
            
            # .nfs files are created when an open file is removed but is still being accessed
            .nfs*
            
            # Log files created by default by the nohup command
            nohup.out
            """
        ),
    ];
}
