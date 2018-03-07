# simple-backup

## A simple backup program for Windows.

[Download](https://simple-backup.djones.co/setup.exe)

### Usage

The program will search for drives that have a `backup-config.json` in their
root file system. The `backup-config.json` file is an array of job objects. Each
job has an array of sources, and a destination. The sources should be an
absolute path. The destination is relative to the drive containing the config
file. Environment variables are expanded on both the sources and the
destination.

### Example `backup-config.json`

```json
[
    {
        "sources": [
            "%userprofile%/Desktop",
            "%userprofile%/Documents"
        ],
        "destination": "backup/%username%"
    }
]
```

This config will copy the users `Desktop` and `Documents` folders to the backup
drive in a folder named after their username in a backup folder in the root of 
the drive. For example, if the backup drive is `F:` and the username is `Alice`,
folders `\backup\Alice\Desktop` and `\backup\Alice\Documents` will be created on
drive `F:`.
