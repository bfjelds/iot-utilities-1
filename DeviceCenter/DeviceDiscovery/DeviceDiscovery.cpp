// DeviceDiscovery.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "DeviceDiscovery.h"

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Devices::Enumeration;

PFN_AddDeviceCallback g_AddCallback = nullptr;

namespace DeviceCenter
{
	CDeviceDiscovery^ CDeviceDiscovery::s_CDeviceDiscoveryObj = nullptr;

	CDeviceDiscovery^ CDeviceDiscovery::GetDeviceDiscoveryObject()
	{
		if (nullptr == s_CDeviceDiscoveryObj)
		{
			s_CDeviceDiscoveryObj = ref new CDeviceDiscovery();
		}
		return s_CDeviceDiscoveryObj;
	}

	CDeviceDiscovery::CDeviceDiscovery()
	{
	}

	// Start Device Discovery
	bool CDeviceDiscovery::Start()
	{
		// Create a query for Enumeration apis
		String^ deviceQueryString = ref new String(L"System.Devices.AepService.ProtocolId:={4526e8c1-8aac-4153-9b16-55e86ada0e54}"
			L"AND System.Devices.Dnssd.Domain:=\"local\""
			L"AND System.Devices.Dnssd.ServiceName:=\"_sshsvc._tcp\"");

		// We want to query HostName, IP Addresses and the mDNS Text Attributes
		Vector<String^>^ queryParameters = ref new Vector<String^>();
		queryParameters->Append(ref new String(L"System.Devices.Dnssd.HostName"));
		queryParameters->Append(ref new String(L"System.Devices.IpAddress"));
		queryParameters->Append(ref new String(L"System.Devices.Dnssd.TextAttributes"));

		// Create the Watcher
		m_mdnsDeviceWatcher = DeviceInformation::CreateWatcher(deviceQueryString, queryParameters, DeviceInformationKind::AssociationEndpointService);

		// Hook-up the Event Handlers
		m_mdnsDeviceWatcher->Updated += ref new TypedEventHandler<DeviceWatcher ^, DeviceInformationUpdate ^>(this, &CDeviceDiscovery::OnUpdate);
		m_mdnsDeviceWatcher->Added += ref new TypedEventHandler<DeviceWatcher ^, DeviceInformation ^>(this, &CDeviceDiscovery::OnAdded);
		// Start Device Discovery 
		m_mdnsDeviceWatcher->Start();
		return true; 
	}

	void CDeviceDiscovery::Stop()
	{
		if (nullptr != m_mdnsDeviceWatcher)
		{
			m_mdnsDeviceWatcher->Stop();
			g_AddCallback = nullptr;
			m_mdnsDeviceWatcher = nullptr;
		}
	}

	void CDeviceDiscovery::OnAdded(DeviceWatcher^ deviceSender, DeviceInformation^ args)
	{
		OutputDebugStringW(args->Name->ToString()->Data());
		if (NULL != g_AddCallback)
		{
			LPWSTR newDeviceNameNative = nullptr;
			LPWSTR newDeviceIPV4AddressNative = nullptr;
			LPWSTR newDeviceIPV6AddressNative = nullptr;
			LPWSTR newDeviceTxtPropertiesNative = nullptr;

			// Get the device Name 
            String^ newDeviceName = args->Properties->Lookup("System.Devices.Dnssd.HostName")->ToString();
			int newDeviceNameLength = wcslen(newDeviceName->Data()) + 1;
			newDeviceNameNative = (LPWSTR)CoTaskMemAlloc(newDeviceNameLength * sizeof(wchar_t));
			if (NULL != newDeviceNameNative)
			{
				if (FAILED(StringCchCopy(newDeviceNameNative, newDeviceNameLength, newDeviceName->Data())))
				{
					return; 
				}
			}

			// Get the IP Address
			auto ipAddressesProperty = args->Properties->Lookup("System.Devices.IpAddress");
			auto ipAddresses = dynamic_cast<IBoxArray<String^>^>(ipAddressesProperty);
			if (nullptr != ipAddresses)
			{
				try
				{
					String^ newDeviceIPV4Address = ipAddresses->Value[0];
					int newDeviceIPV4AddressLength = wcslen(newDeviceIPV4Address->Data()) + 1;
					newDeviceIPV4AddressNative = (LPWSTR)CoTaskMemAlloc(newDeviceIPV4AddressLength * sizeof(wchar_t));
					if (NULL != newDeviceIPV4AddressNative)
					{
						if (FAILED(StringCchCopy(newDeviceIPV4AddressNative, newDeviceIPV4AddressLength, newDeviceIPV4Address->Data())))
						{
							return;
						}
					}

					String^ newDeviceIPV6Address = ipAddresses->Value[1];
					int newDeviceIPV6AddressLength = wcslen(newDeviceIPV6Address->Data()) + 1;
					newDeviceIPV6AddressNative = (LPWSTR)CoTaskMemAlloc(newDeviceIPV6AddressLength * sizeof(wchar_t));
					if (NULL != newDeviceIPV6AddressNative)
					{
						if (FAILED(StringCchCopy(newDeviceIPV6AddressNative, newDeviceIPV6AddressLength, newDeviceIPV6Address->Data())))
						{
							return;
						}
					}
				}
				catch(Exception^ exp)
				{
					return; 
				}
			}
				
			// Add the additional TXT properties 
			String^ newDeviceTxtProperties = ref new String(L"");
			auto textAttributesProperty = args->Properties->Lookup("System.Devices.Dnssd.TextAttributes");
			auto textAttributes = dynamic_cast<IBoxArray<String^>^>(textAttributesProperty);
			for (unsigned int i = 0; i < textAttributes->Value->Length; i++)
			{
				newDeviceTxtProperties += textAttributes->Value[i];
				newDeviceTxtProperties += L",";
			}

			int newDeviceTxtPropertiesLength = wcslen(newDeviceTxtProperties->Data()) + 1;
			newDeviceTxtPropertiesNative = (LPWSTR)CoTaskMemAlloc(newDeviceTxtPropertiesLength * sizeof(wchar_t));
			if (NULL != newDeviceTxtPropertiesNative)
			{
				if (FAILED(StringCchCopy(newDeviceTxtPropertiesNative, newDeviceTxtPropertiesLength, newDeviceTxtProperties->Data())))
				{
					return;
				}
			}

			g_AddCallback(newDeviceNameNative, newDeviceIPV4AddressNative, newDeviceIPV6AddressNative, newDeviceTxtPropertiesNative);
		}
	}

	void CDeviceDiscovery::OnUpdate(DeviceWatcher^ deviceSender, DeviceInformationUpdate^ args)
	{
	}
}

DEVICEDISCOVERY_API void __cdecl RegisterCallback(PFN_AddDeviceCallback callback)
{
	g_AddCallback = callback;
}

DEVICEDISCOVERY_API bool __cdecl StartDiscovery()
{
	DeviceCenter::CDeviceDiscovery^ discoveryObject = DeviceCenter::CDeviceDiscovery::GetDeviceDiscoveryObject();
	return discoveryObject->Start();
}
DEVICEDISCOVERY_API void __cdecl StopDiscovery()
{
	DeviceCenter::CDeviceDiscovery^ discoveryObject = DeviceCenter::CDeviceDiscovery::GetDeviceDiscoveryObject();
	discoveryObject->Stop();
}
