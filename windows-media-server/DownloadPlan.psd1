@{
    MediaRoot = 'E:\MediaServer'
    AdminAddress = '127.0.0.1'
    ProwlarrPort = 9696

    DownloadClients = @(
        @{ Name = 'qBittorrent'; Port = 8080; Folder = 'Torrents' }
        @{ Name = 'NZBGet'; Port = 6789; Folder = 'Usenet' }
    )

    Routes = @(
        @{ Name = 'Sonarr'; Port = 8989; Category = 'tv'; Library = 'TV' }
        @{ Name = 'Radarr'; Port = 7878; Category = 'movies'; Library = 'Movies' }
        @{ Name = 'Radarr Small Movies'; Port = 7879; Category = 'movies-small'; Library = 'Small Movies' }
    )
}
