namespace HELIOS.Platform.Core.RemoteAccess.Models;

/// <summary>
/// Represents remote connection information and metadata.
/// </summary>
public class RemoteConnectionInfo
{
    /// <summary>Gets or sets the unique connection ID.</summary>
    public string ConnectionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the session ID for multi-user support.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the remote host address.</summary>
    public string RemoteHost { get; set; } = string.Empty;

    /// <summary>Gets or sets the remote port.</summary>
    public int RemotePort { get; set; }

    /// <summary>Gets or sets the connection protocol type.</summary>
    public ConnectionProtocol Protocol { get; set; } = ConnectionProtocol.SSH;

    /// <summary>Gets or sets the connection state.</summary>
    public ConnectionState State { get; set; } = ConnectionState.Disconnected;

    /// <summary>Gets or sets the encrypted credentials.</summary>
    public EncryptedCredentials? Credentials { get; set; }

    /// <summary>Gets or sets the connection creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last activity timestamp.</summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the connection timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Gets or sets whether the connection uses secure tunneling.</summary>
    public bool UseSecureTunneling { get; set; } = true;

    /// <summary>Gets or sets the TLS version for secure connections.</summary>
    public string? TlsVersion { get; set; }

    /// <summary>Gets or sets the cipher suite for encryption.</summary>
    public string? CipherSuite { get; set; }

    /// <summary>Gets or sets the user identifier for the connection.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection metadata.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Enumeration of supported connection protocols.
/// </summary>
public enum ConnectionProtocol
{
    /// <summary>SSH protocol</summary>
    SSH,

    /// <summary>VPN protocol</summary>
    VPN,

    /// <summary>HTTPS/REST protocol</summary>
    HTTPS,

    /// <summary>WebSocket protocol</summary>
    WebSocket,

    /// <summary>RDP protocol</summary>
    RDP
}

/// <summary>
/// Enumeration of connection states.
/// </summary>
public enum ConnectionState
{
    /// <summary>Connection disconnected</summary>
    Disconnected,

    /// <summary>Connection in progress</summary>
    Connecting,

    /// <summary>Connection active</summary>
    Connected,

    /// <summary>Connection suspended</summary>
    Suspended,

    /// <summary>Connection error state</summary>
    Error
}

/// <summary>
/// Represents encrypted credentials for remote connections.
/// </summary>
public class EncryptedCredentials
{
    /// <summary>Gets or sets the encrypted username.</summary>
    public string EncryptedUsername { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted password.</summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted private key.</summary>
    public string? EncryptedPrivateKey { get; set; }

    /// <summary>Gets or sets the encryption algorithm used.</summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>Gets or sets the salt used in encryption.</summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>Gets or sets the IV (initialization vector).</summary>
    public string IV { get; set; } = string.Empty;
}
