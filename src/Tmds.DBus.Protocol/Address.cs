namespace Tmds.DBus.Protocol;

/// <summary>
/// Provides D-Bus addresses for the system and session bus.
/// </summary>
[Obsolete("Use DBusAddress instead.")]
public static class Address
{
    /// <summary>
    /// Gets the D-Bus system bus address.
    /// </summary>
    public static string? System
        => DBusAddress.System;

    /// <summary>
    /// Gets the D-Bus session bus address.
    /// </summary>
    public static string? Session
        => DBusAddress.Session;
}
