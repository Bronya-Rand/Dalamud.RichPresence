# Discord Rich Presence

A Final Fantasy XIV plugin that integrates Discord's Rich Presence into the game.

## Features
- Support for Windows and Linux\* and macOS\*\*
    > \* - Some versions of Wine/Proton may require the use of an external TCP bridge in order to function properly. See [Additional Setup for Linux/macOS Users](#additional-setup-for-linuxmac-users).
    >
    > \*\* - Support for macOS is experimental and requires the use of an external TCP bridge in order to function properly. See [Additional Setup for Linux/macOS Users](#additional-setup-for-linuxmac-users).

- Custom field data! Setup what you want to display in your RPC status.
- IPC integration with Waitingway to show login queue position.

## Additional Setup for Linux/macOS Users
> If you are running the official XIVLauncher Wine build (Wine 10.8) or a Wine/Proton build that supports `AF_UNIX`, you do not need to do anything below. The plugin will connect to Discord by itself.

Make sure that the `RichPresenceBridge` binary is executable. You can do this by copying the path provided in the Configuration Window (using the "Copy Binaries Folder Path" button) and running the following command in your terminal in that directory:

### Linux
```bash
chmod +x ./linux-x64/RichPresenceBridge
```

### macOS
```bash
chmod +x ./osx-arm64/RichPresenceBridge
```

1. Launch the Rich Presence Bridge for your platform on your computer (not within Wine/Proton).

    **Linux**
    ```bash
    ./linux-x64/RichPresenceBridge
    ```

    **macOS**
    ```bash
    ./osx-arm64/RichPresenceBridge
    ```

2. Launch XIV.

If all is well, you should see the game you are playing as **FINAL FANTASY XIV** on Discord with added information instead of just **FINAL FANTASY XIV Online**.