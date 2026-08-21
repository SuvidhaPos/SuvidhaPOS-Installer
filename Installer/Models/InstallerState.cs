using System;
using System.Collections.Generic;

namespace SuvidhaPOSInstaller.DynamicUI.Models;

public sealed class InstallerState
{
    public int CurrentStep { get; set; } = 1;
    public string InstallPath { get; set; } = @"D:\Suvidha Pos\Software";
    public string AppVersion { get; set; } = "2.3.1.0";
    public DateTime? InstallationDate { get; set; }
    public string? BackupPath { get; set; }
    public bool SetupCompleted { get; set; }
    public string? Error { get; set; }
    public List<InstallerComponent> Components { get; set; } = new();
    public DownloadState Download { get; set; } = new();
    public InstallationState Installation { get; set; } = new();
    public DatabaseState Database { get; set; } = new();
    public long TotalDownloadBytes => Download.TotalBytes;
    public long TotalInstalledBytes { get; set; }
}

public sealed class InstallerComponent
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "Exe";
    public string Status { get; set; } = "Pending";
    public string? Error { get; set; }
    public long SizeBytes { get; set; }
    public bool Selected { get; set; } = true;
    public int Progress { get; set; }
}

public sealed class DownloadState
{
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public int Percentage => TotalBytes <= 0 ? 0 : Math.Clamp((int)(DownloadedBytes * 100L / TotalBytes), 0, 100);
    public TimeSpan RemainingTime { get; set; }
    public string Location { get; set; } = "";
}

public sealed class InstallationState
{
    public long InstalledBytes { get; set; }
    public long TotalBytes { get; set; }
    public int Percentage => TotalBytes <= 0 ? 0 : Math.Clamp((int)(InstalledBytes * 100L / TotalBytes), 0, 100);
    public TimeSpan RemainingTime { get; set; }
    public string Location { get; set; } = "";
}

public sealed class DatabaseState
{
    public string ServerName { get; set; } = "";
    public string Authentication { get; set; } = "Windows Authentication";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string DatabaseName { get; set; } = "SuvidhaPOS";
    public string Collation { get; set; } = "SQL_Latin1_General_CP1_CI_AS";
    public bool ConnectionSuccessful { get; set; }
    public bool RestoreRequested { get; set; }
    public string RestoreStatus { get; set; } = "";
}