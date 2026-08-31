/// Backend connection target.
///
/// Switch [target] to match how you're running the app during development:
///
/// - [ApiTarget.androidEmulator]: 10.0.2.2 is the emulator's alias for the
///   host machine's localhost. Plain `localhost` inside the emulator
///   resolves to the emulator itself, not your dev machine.
/// - [ApiTarget.physicalDevice]: set [lanIp] to your machine's LAN IP
///   (e.g. `ipconfig` on Windows) and make sure the phone is on the same
///   Wi-Fi network as the backend.
/// - [ApiTarget.iosSimulator]: the iOS simulator shares the host's network
///   namespace, so `localhost` works directly.
enum ApiTarget { androidEmulator, physicalDevice, iosSimulator }

abstract final class ApiConfig {
  /// Change this to match your current run target.
  static const ApiTarget target = ApiTarget.androidEmulator;

  /// Fill this in with your machine's LAN IP when using
  /// [ApiTarget.physicalDevice] (e.g. "192.168.1.23").
  static const String lanIp = '192.168.1.23';

  static const int port = 7005;

  static String get baseUrl {
    switch (target) {
      case ApiTarget.androidEmulator:
        return 'https://10.0.2.2:$port';
      case ApiTarget.physicalDevice:
        return 'https://$lanIp:$port';
      case ApiTarget.iosSimulator:
        return 'https://localhost:$port';
    }
  }

  static const String apiPrefix = '/api';

  static String get apiBaseUrl => '$baseUrl$apiPrefix';

  /// SignalR tracking hub — see API reference §25.
  static String get trackingHubUrl => '$baseUrl/hubs/tracking';
}
