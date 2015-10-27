#ifdef DEVICEDISCOVERY_EXPORTS
#define DEVICEDISCOVERY_API  extern "C" __declspec(dllexport)
#else
#define DEVICEDISCOVERY_API __declspec(dllimport)
#endif

typedef void(CALLBACK *PFN_AddDeviceCallback)(LPWSTR, LPWSTR, LPWSTR, LPWSTR);

namespace DeviceCenter
{
	// This class is exported from the DeviceDiscovery.dll
	public ref class CDeviceDiscovery sealed {
	public:
		CDeviceDiscovery(void);
		bool Start();
		void Stop();
		static CDeviceDiscovery^ GetDeviceDiscoveryObject();

	private:
		void OnAdded(Windows::Devices::Enumeration::DeviceWatcher^ watcher, Windows::Devices::Enumeration::DeviceInformation^ information);
		void OnUpdate(Windows::Devices::Enumeration::DeviceWatcher^ watcher, Windows::Devices::Enumeration::DeviceInformationUpdate^ information);

		// DNS-SD Watcher
		Windows::Devices::Enumeration::DeviceWatcher^ m_mdnsDeviceWatcher;

		// Singleton
		static CDeviceDiscovery^ s_CDeviceDiscoveryObj;
	};
}

DEVICEDISCOVERY_API void __cdecl RegisterCallback(PFN_AddDeviceCallback);
DEVICEDISCOVERY_API bool __cdecl StartDiscovery();
DEVICEDISCOVERY_API void __cdecl StopDiscovery();

