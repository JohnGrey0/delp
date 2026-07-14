using System.Globalization;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>One well-known or commonly-used network port.</summary>
public sealed record PortEntry(int Port, string Protocol, string Service, string Description);

/// <summary>Reference data for TCP/UDP ports: IANA well-known ports plus registered ports developers hit day to day.</summary>
public static class PortData
{
    public static IReadOnlyList<PortEntry> All { get; } = BuildAll();

    /// <summary>Matches an exact or prefix port number, or a substring of the service/description.</summary>
    public static IReadOnlyList<PortEntry> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return All;

        var q = query.Trim();
        return All.Where(e =>
                e.Port.ToString(CultureInfo.InvariantCulture).StartsWith(q, StringComparison.OrdinalIgnoreCase) ||
                e.Service.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static List<PortEntry> BuildAll()
    {
        const string Tcp = "TCP", Udp = "UDP", Both = "TCP/UDP";

        return new List<PortEntry>
        {
            new(7, Both, "Echo", "Echoes back whatever the client sends; used for basic reachability testing."),
            new(9, Both, "Discard", "Silently discards anything sent to it; used to test outbound connectivity."),
            new(13, Both, "Daytime", "Returns the current date and time as readable text."),
            new(17, Both, "QOTD", "Quote of the Day — returns a short text quote."),
            new(19, Both, "Chargen", "Character Generator — streams a repeating character sequence; used for load testing."),
            new(20, Tcp, "FTP (Data)", "File Transfer Protocol data channel, used for the actual file transfer in active mode."),
            new(21, Tcp, "FTP (Control)", "File Transfer Protocol command channel — login and command negotiation."),
            new(22, Tcp, "SSH", "Secure Shell — encrypted remote login, command execution, SFTP, and tunneling."),
            new(23, Tcp, "Telnet", "Unencrypted remote terminal access; considered insecure for anything but legacy/lab use."),
            new(25, Tcp, "SMTP", "Simple Mail Transfer Protocol — server-to-server mail relay."),
            new(37, Both, "Time", "Returns the current time as a 32-bit binary value (seconds since 1900)."),
            new(42, Both, "WINS / Host Name Server", "Windows Internet Name Service, or the older ARPA Host Name Server protocol."),
            new(43, Tcp, "WHOIS", "Looks up registration information for a domain name or IP address block."),
            new(49, Both, "TACACS", "Terminal Access Controller Access-Control System — legacy AAA protocol for network devices."),
            new(53, Both, "DNS", "Domain Name System — resolves names to addresses; UDP for lookups, TCP for zone transfers and large responses."),
            new(67, Udp, "DHCP (Server)", "Dynamic Host Configuration Protocol — server side, hands out IP leases."),
            new(68, Udp, "DHCP (Client)", "Dynamic Host Configuration Protocol — client side, receives IP lease offers."),
            new(69, Udp, "TFTP", "Trivial File Transfer Protocol — simple, unauthenticated file transfer used by network boot/firmware flows."),
            new(70, Tcp, "Gopher", "Pre-web hierarchical document retrieval protocol; mostly historical today."),
            new(79, Tcp, "Finger", "Returns information about users on a remote system; largely deprecated for privacy reasons."),
            new(80, Tcp, "HTTP", "Hypertext Transfer Protocol — unencrypted web traffic."),
            new(88, Both, "Kerberos", "Network authentication protocol using tickets, widely used in Active Directory environments."),
            new(110, Tcp, "POP3", "Post Office Protocol v3 — downloads mail from a server to a client, unencrypted."),
            new(111, Both, "RPCbind / Portmapper", "Maps ONC RPC program numbers to the port they're listening on, used by NFS and related services."),
            new(113, Tcp, "Ident", "Identification Protocol — used by some IRC/mail servers to identify the remote user of a TCP connection."),
            new(119, Tcp, "NNTP", "Network News Transfer Protocol — Usenet article retrieval and posting."),
            new(123, Udp, "NTP", "Network Time Protocol — clock synchronization."),
            new(135, Both, "MS RPC Endpoint Mapper", "Microsoft RPC locator service used by many Windows services (DCOM, WMI, etc.)."),
            new(137, Both, "NetBIOS Name Service", "Resolves NetBIOS names to addresses on a local Windows network."),
            new(138, Udp, "NetBIOS Datagram", "Connectionless NetBIOS service used for browsing and messaging."),
            new(139, Tcp, "NetBIOS Session", "Connection-oriented NetBIOS service, historically used for SMB before direct TCP/445."),
            new(143, Tcp, "IMAP", "Internet Message Access Protocol — manages mail on the server; unencrypted."),
            new(161, Udp, "SNMP", "Simple Network Management Protocol — polls device metrics and configuration."),
            new(162, Udp, "SNMP Trap", "Receives unsolicited SNMP notifications (traps) pushed from managed devices."),
            new(177, Udp, "XDMCP", "X Display Manager Control Protocol — remote X11 login sessions."),
            new(179, Tcp, "BGP", "Border Gateway Protocol — exchanges routing information between autonomous systems on the internet."),
            new(194, Tcp, "IRC", "Internet Relay Chat — plaintext chat protocol."),
            new(389, Both, "LDAP", "Lightweight Directory Access Protocol — queries directory services such as Active Directory, unencrypted."),
            new(427, Both, "SLP", "Service Location Protocol — discovers services on a local network."),
            new(443, Tcp, "HTTPS", "HTTP over TLS — encrypted web traffic; also used as a base for HTTP/2 and QUIC/HTTP/3 negotiation."),
            new(445, Tcp, "SMB / Microsoft-DS", "Server Message Block — Windows file/printer sharing directly over TCP (no NetBIOS)."),
            new(464, Both, "Kerberos Change/Set Password", "Sub-protocol of Kerberos for changing or setting a principal's password."),
            new(465, Tcp, "SMTPS (Submission over TLS)", "SMTP mail submission wrapped in implicit TLS from the start of the connection."),
            new(500, Udp, "IKE / ISAKMP", "Internet Key Exchange — negotiates and manages IPsec VPN security associations."),
            new(513, Tcp, "rlogin", "Legacy remote login protocol from BSD Unix; unencrypted and considered insecure."),
            new(514, Udp, "Syslog", "Sends log messages to a centralized logging server."),
            new(515, Tcp, "LPD / Printer", "Line Printer Daemon protocol — classic Unix network printing."),
            new(520, Udp, "RIP", "Routing Information Protocol — distance-vector routing for small IPv4 networks."),
            new(521, Udp, "RIPng", "Routing Information Protocol for IPv6 networks."),
            new(548, Tcp, "AFP", "Apple Filing Protocol — legacy macOS network file sharing."),
            new(554, Both, "RTSP", "Real Time Streaming Protocol — controls streaming media sessions (used by IP cameras, media servers)."),
            new(587, Tcp, "SMTP Submission", "Mail submission from clients to their outgoing mail server, typically with authentication and STARTTLS."),
            new(631, Both, "IPP / CUPS", "Internet Printing Protocol — used by CUPS for print job submission and printer discovery."),
            new(636, Tcp, "LDAPS", "LDAP over implicit TLS."),
            new(639, Tcp, "MSDP", "Multicast Source Discovery Protocol — shares active multicast sources between routing domains."),
            new(860, Tcp, "iSCSI", "Internet Small Computer Systems Interface — block storage over IP networks."),
            new(873, Tcp, "rsync", "Efficient file synchronization protocol, used both standalone and as an SSH subsystem."),
            new(902, Tcp, "VMware Server", "VMware ESXi/vCenter host agent management interface."),
            new(989, Tcp, "FTPS (Data)", "FTP data channel secured with implicit TLS."),
            new(990, Tcp, "FTPS (Control)", "FTP command channel secured with implicit TLS."),
            new(993, Tcp, "IMAPS", "IMAP over implicit TLS."),
            new(995, Tcp, "POP3S", "POP3 over implicit TLS."),
            new(1080, Tcp, "SOCKS Proxy", "Generic proxy protocol that relays arbitrary TCP (and, with SOCKS5, UDP) traffic."),
            new(1194, Udp, "OpenVPN", "Default port for the OpenVPN VPN protocol."),
            new(1352, Tcp, "Lotus Notes / Domino", "IBM/HCL Notes RPC protocol for mail and collaboration."),
            new(1433, Tcp, "Microsoft SQL Server", "Default port for Microsoft SQL Server's tabular data stream (TDS) protocol."),
            new(1434, Udp, "SQL Server Browser", "Resolves named SQL Server instances to their dynamic TCP port."),
            new(1521, Tcp, "Oracle DB Listener", "Default port for Oracle Database's TNS listener."),
            new(1701, Udp, "L2TP", "Layer 2 Tunneling Protocol — often paired with IPsec for VPNs."),
            new(1723, Tcp, "PPTP", "Point-to-Point Tunneling Protocol — legacy, cryptographically weak VPN protocol."),
            new(1812, Udp, "RADIUS Authentication", "Authenticates and authorizes network access requests (Wi-Fi, VPN, switches)."),
            new(1813, Udp, "RADIUS Accounting", "Collects usage/accounting records for RADIUS-authenticated sessions."),
            new(1883, Tcp, "MQTT", "Lightweight publish/subscribe messaging protocol, common in IoT."),
            new(1900, Udp, "SSDP / UPnP", "Simple Service Discovery Protocol — discovers UPnP devices on a local network."),
            new(2049, Both, "NFS", "Network File System — Unix/Linux network file sharing."),
            new(2082, Tcp, "cPanel", "Default cPanel web hosting control panel port (unencrypted)."),
            new(2083, Tcp, "cPanel (SSL)", "cPanel control panel over TLS."),
            new(2181, Tcp, "ZooKeeper", "Client port for Apache ZooKeeper, used for distributed coordination (Kafka, Hadoop, etc.)."),
            new(2222, Tcp, "SSH (Alternate)", "Common alternate SSH port used to avoid the noise of automated scans on 22, e.g. in Docker/hosting setups."),
            new(2375, Tcp, "Docker Daemon (Unencrypted)", "Docker Engine API without TLS — should never be exposed outside a trusted host."),
            new(2376, Tcp, "Docker Daemon (TLS)", "Docker Engine API secured with mutual TLS."),
            new(2379, Tcp, "etcd (Client)", "Client API port for the etcd distributed key-value store, used by Kubernetes."),
            new(2380, Tcp, "etcd (Peer)", "Peer-to-peer port used for etcd cluster consensus traffic."),
            new(3000, Tcp, "Common Dev Server", "Conventional default port for local development servers (Node/Express, Rails, React, Grafana, etc.)."),
            new(3128, Tcp, "Squid Proxy", "Default port for the Squid caching HTTP proxy."),
            new(3260, Tcp, "iSCSI Target", "Port used by iSCSI target (storage server) listeners for incoming connections."),
            new(3268, Tcp, "Active Directory Global Catalog", "LDAP queries against the forest-wide Global Catalog."),
            new(3269, Tcp, "Active Directory Global Catalog (SSL)", "Global Catalog LDAP over TLS."),
            new(3306, Tcp, "MySQL / MariaDB", "Default port for MySQL and MariaDB database connections."),
            new(3389, Tcp, "RDP", "Remote Desktop Protocol — Windows remote graphical desktop access."),
            new(3690, Tcp, "Subversion (svn)", "Default port for the svn:// protocol used by Apache Subversion."),
            new(4200, Tcp, "Angular CLI Dev Server", "Default port for `ng serve`, the Angular CLI's local development server."),
            new(4369, Tcp, "EPMD", "Erlang Port Mapper Daemon — resolves node names to ports for distributed Erlang/Elixir apps (e.g. RabbitMQ clustering)."),
            new(4500, Udp, "IPsec NAT-T", "IPsec traffic encapsulation used to traverse NAT gateways."),
            new(5000, Tcp, "Common Dev Server", "Conventional default port for local development servers (Flask, ASP.NET Core, Synology DSM, etc.)."),
            new(5044, Tcp, "Logstash Beats Input", "Port Logstash listens on for Beats shippers (Filebeat, Metricbeat) in the Elastic Stack."),
            new(5060, Both, "SIP", "Session Initiation Protocol — sets up VoIP and video calls, unencrypted signaling."),
            new(5061, Tcp, "SIP-TLS", "SIP signaling over TLS."),
            new(5173, Tcp, "Vite Dev Server", "Default port for the Vite frontend build tool's local development server."),
            new(5222, Tcp, "XMPP (Client)", "Client-to-server connections for the XMPP (Jabber) messaging protocol."),
            new(5269, Tcp, "XMPP (Server)", "Server-to-server federation connections for XMPP."),
            new(5353, Udp, "mDNS", "Multicast DNS — local network name resolution used by Bonjour/Avahi (`.local` hostnames)."),
            new(5432, Tcp, "PostgreSQL", "Default port for PostgreSQL database connections."),
            new(5555, Tcp, "Android ADB", "Android Debug Bridge — connects to an Android device or emulator over TCP."),
            new(5601, Tcp, "Kibana", "Web UI port for Kibana, the visualization layer of the Elastic Stack."),
            new(5672, Tcp, "AMQP / RabbitMQ", "Advanced Message Queuing Protocol — default RabbitMQ client connection port."),
            new(5900, Tcp, "VNC", "Virtual Network Computing — remote graphical desktop access."),
            new(5938, Tcp, "TeamViewer", "Default outbound port used by the TeamViewer remote-access client."),
            new(5984, Tcp, "CouchDB", "HTTP API port for Apache CouchDB."),
            new(6000, Tcp, "X11", "Base port for the X Window System display server (display :0)."),
            new(6379, Tcp, "Redis", "Default port for the Redis in-memory data store."),
            new(6443, Tcp, "Kubernetes API Server", "Default port for the Kubernetes control-plane API server."),
            new(6667, Tcp, "IRC", "Common plaintext IRC server port."),
            new(6881, Both, "BitTorrent", "Common default port range start for BitTorrent peer connections."),
            new(7000, Tcp, "Cassandra (Inter-node)", "Default port for unencrypted Cassandra cluster (gossip) communication."),
            new(7001, Tcp, "Cassandra (Inter-node, TLS)", "Cassandra inter-node communication secured with TLS."),
            new(7474, Tcp, "Neo4j HTTP", "Neo4j graph database's HTTP/browser interface."),
            new(7687, Tcp, "Neo4j Bolt", "Neo4j's binary Bolt protocol used by official drivers."),
            new(8000, Tcp, "Common Dev Server", "Conventional default port for local development servers (Django, Python `http.server`, etc.)."),
            new(8080, Tcp, "HTTP Alternate", "Common alternate HTTP port for proxies and app servers such as Tomcat and Jenkins."),
            new(8081, Tcp, "HTTP Alternate", "Secondary alternate HTTP port, often used for admin UIs or a second app instance."),
            new(8086, Tcp, "InfluxDB", "HTTP API port for the InfluxDB time-series database."),
            new(8091, Tcp, "Couchbase Web Console", "Administration UI and REST API for Couchbase Server."),
            new(8200, Tcp, "HashiCorp Vault", "API and UI port for HashiCorp Vault secrets management."),
            new(8443, Tcp, "HTTPS Alternate", "Common alternate HTTPS port, e.g. Tomcat's SSL connector or Kubernetes dashboards."),
            new(8500, Tcp, "HashiCorp Consul", "HTTP API and UI port for Consul service discovery."),
            new(8888, Tcp, "Jupyter Notebook", "Default port for the Jupyter Notebook/Lab web interface."),
            new(9000, Tcp, "PHP-FPM / SonarQube / MinIO", "Commonly reused default port across several dev tools — check what's actually bound before assuming."),
            new(9042, Tcp, "Cassandra CQL", "Native client protocol port for querying Apache Cassandra."),
            new(9090, Tcp, "Prometheus", "Web UI and HTTP API port for the Prometheus monitoring server."),
            new(9092, Tcp, "Kafka", "Default broker port for Apache Kafka client connections."),
            new(9100, Tcp, "Printer / JetDirect", "Raw TCP printing port used by network printers (HP JetDirect and compatible)."),
            new(9200, Tcp, "Elasticsearch HTTP", "REST API port for Elasticsearch."),
            new(9300, Tcp, "Elasticsearch Transport", "Internal node-to-node transport port for an Elasticsearch cluster."),
            new(9418, Tcp, "Git Protocol", "Anonymous, unencrypted `git://` transport for read-only repository access."),
            new(11211, Both, "Memcached", "Default port for the Memcached distributed memory object cache."),
            new(15672, Tcp, "RabbitMQ Management UI", "Web-based management console and HTTP API for RabbitMQ."),
            new(25565, Tcp, "Minecraft (Java Edition)", "Default port for a Minecraft Java Edition multiplayer server."),
            new(27017, Tcp, "MongoDB", "Default port for MongoDB database connections."),
            new(27018, Tcp, "MongoDB (Shard)", "Default port for a MongoDB shard server in a sharded cluster."),
            new(28015, Tcp, "RethinkDB", "Client driver port for the RethinkDB database."),
        };
    }
}
