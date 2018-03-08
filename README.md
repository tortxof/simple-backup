# simple-backup

## A simple backup program for Windows.

[Download](https://simple-backup.djones.co/setup.exe)

[Download sample backup-config.json](https://raw.githubusercontent.com/tortxof/simple-backup/master/backup-config.json)

[Download sample backup-config.txt](https://raw.githubusercontent.com/tortxof/simple-backup/master/backup-config.txt)

### Usage

The program will search for drives that have a `backup-config.json` or
`backup-config.txt` in their root file system.

The `backup-config.json` file is an array of job objects. Each job has an array
of sources, and a destination. Each source should be an absolute path. The
destination is relative to the drive containing the config file. Environment
variables are expanded on both the sources and the destination.

The `backup-config.txt` file is used if a json file is not found. Lines
beginning with `#` are comments and are ignored. Empty lines are also ignored.
The first non comment line is the destination, and all other lines are sources.
This means with a text based config, you are limited to one job.

### Example

The following two config examples are identical.

#### `backup-config.json`

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

#### `backup-config.txt`

```
# Destination
backup\%username%

# Sources
%userprofile%\Desktop
%userprofile%\Documents
```

This config will copy the user's `Desktop` and `Documents` folders to the backup
drive in a folder named after their username in a backup folder in the root of
the drive. For example, if the backup drive is `F:` and the username is `Alice`,
folders `\backup\Alice\Desktop` and `\backup\Alice\Documents` will be created on
drive `F:`.
